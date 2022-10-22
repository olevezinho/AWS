using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.RDS;
using Amazon.CDK.AWS.AutoScaling;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.SSM;
using Amazon.CDK.AWS.CodeDeploy;
using Amazon.CDK.AWS.CodePipeline;
using Amazon.CDK.AWS.CodePipeline.Actions;
using Amazon.CDK.AWS.CloudTrail;
using Amazon.CDK.AWS.S3;

namespace GigsNearMeInfra
{
    public class GigsNearMeStack : Stack
    {
        // the name of the file in S3 containing the CodeDeploy-bundled app, to trigger the pipeline
        const string DeploymentArtifactName = "GigsNearMeDeploymentBundle.zip";
        const double dbPort = 1433;

        internal GigsNearMeStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            #region Application Hosting Resources

            // Create a new vpc spanning two AZs and with public and private subnets
            // to host the application resources
            var vpc = new Vpc(this, "Vpc", new VpcProps
            {
                Cidr = "10.0.0.0/16",
                MaxAzs = 2,
                NatGateways = 1,
                SubnetConfiguration = new SubnetConfiguration[]
                {
                    new SubnetConfiguration
                    {
                        CidrMask = 24,
                        SubnetType = SubnetType.PUBLIC,
                        Name = "Public"
                    },
                    new SubnetConfiguration
                    {
                        CidrMask = 24,
                        SubnetType = SubnetType.PRIVATE,
                        Name = "Private"
                    }
                }
            });

            // place a SQL Server Express database into one of the private subnets
            var db = new DatabaseInstance(this, "DB", new DatabaseInstanceProps
            {
                Vpc = vpc,
                VpcSubnets = new SubnetSelection
                {
                    SubnetType = SubnetType.PRIVATE
                },
                // SQL Server 2017 Express Edition, in conjunction with a db.t2.micro instance type,
                // fits inside the free tier for new accounts
                Engine = DatabaseInstanceEngine.SqlServerEx(new SqlServerExInstanceEngineProps
                {
                    Version = SqlServerEngineVersion.VER_14
                }),
                InstanceType = InstanceType.Of(InstanceClass.BURSTABLE2, InstanceSize.MICRO),
                Port = dbPort,
                InstanceIdentifier = "gigsnearmedb",
                // turn off automated backups so that this sample launches a little faster by
                // avoiding an initial backup :-)
                BackupRetention = Duration.Seconds(0)
            });

            // Parameter Store entry that points the production app to the Secrets Manager
            // secret holding details of the database in RDS; the production app will
            // retreve the secret and use to build the connection string
            new StringParameter(this, "GigsNearMeDbSecretsParameter", new StringParameterProps
            {
                ParameterName = "/gigsnearme/dbsecretsname",
                StringValue = db.Secret.SecretName
            });

            // create a role for the application
            var appRole = new Role(this, "InstanceRole", new RoleProps
            {
                AssumedBy = new ServicePrincipal("ec2.amazonaws.com"),
                ManagedPolicies = new IManagedPolicy[]
                {
                    ManagedPolicy.FromAwsManagedPolicyName("AmazonSSMManagedInstanceCore"),
                    ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSCodeDeployRole")
                }
            });

            // access to read parameters by path is not in the AmazonSSMManagedInstanceCore
            // managed policy
            appRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
            {
                Effect = Effect.ALLOW,
                Actions = new string[] { "ssm:GetParametersByPath" },
                Resources = new string[]
                {
                    Arn.Format(new ArnComponents
                    {
                        Service = "ssm",
                        Resource = "parameter",
                        ResourceName = "gigsnearme/*"
                    }, this)
                }
            }));

            // the app also needs to retrieve the database password, etc, posted automatically
            // to Secrets Manager
            db.Secret.GrantRead(appRole);

            // create an autoscaling group for the web server(s), placing them into private subnets
            var scalingGroup = new AutoScalingGroup(this, "ASG", new AutoScalingGroupProps
            {
                Vpc = vpc,
                VpcSubnets = new SubnetSelection
                {
                    SubnetType = SubnetType.PRIVATE
                },
                // since we don't hold any data on the instance, and install what we need on startup,
                // we just always launch the latest version
                MachineImage = MachineImage.LatestAmazonLinux(new AmazonLinuxImageProps
                {
                    Generation = AmazonLinuxGeneration.AMAZON_LINUX_2
                }),
                InstanceType = InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.MICRO),
                MinCapacity = 1,
                MaxCapacity = 2,
                AllowAllOutbound = true,
                Role = appRole,
                Signals = Signals.WaitForCount(1, new SignalsOptions
                {
                    Timeout = Duration.Minutes(10)
                }),
            });

            // Initialize the vanilla EC2 instances with our needed software at launch
            // time using a UserData script. Because the instances will not be exposed
            // to the internet, and will only be accessible behind the load balancer
            // we'll use Kestrel directly and not configure a reverse proxy setup with
            // Nginx or Apache.
            scalingGroup.AddUserData(new string[]
            {
                "yum -y update",
                // install .NET 5 and ASP.NET Core packages
                "rpm -Uvh https://packages.microsoft.com/config/centos/7/packages-microsoft-prod.rpm",
                "yum -y install dotnet-runtime-5.0",
                "yum -y install aspnetcore-runtime-5.0",
                // install the CodeDeploy agent and dependencies
                "yum -y install ruby",
                "yum -y install wget",
                "cd /tmp",
                $"wget https://aws-codedeploy-{props.Env.Region}.s3.{props.Env.Region}.amazonaws.com/latest/install",
                "chmod +x ./install",
                "./install auto",
                "service codedeploy-agent start",
            });

            // Deploy a simple placeholder app, to give a nicer experience than a '502 Bad Gateway'
            // error should the user access the load balancer URL before deployment of the real app.
            // You can remove after you have the CI/CD pipeline set up.
            scalingGroup.AddUserData(new string[]
            {
              "curl https://aws-tc-largeobjects.s3-us-west-2.amazonaws.com/Curation/DotNet/CDK/deploySampleApp.sh | bash"
            });

            // add one final command, to signal when the instance has completed the script and is ready
            // to proceed
            scalingGroup.UserData.AddSignalOnExitCommand(scalingGroup);

            // ensure our EC2 instances can reach the database
            db.Connections.AllowFrom(scalingGroup, Port.Tcp(dbPort));

            // Configure an application load balancer, listening on port 80, to front our fleet
            var loadBalancer = new ApplicationLoadBalancer(this, "LB", new ApplicationLoadBalancerProps
            {
                Vpc = vpc,
                InternetFacing = true
            });

            var lbListener = loadBalancer.AddListener("HttpListener", new BaseApplicationListenerProps
            {
                Port = 80,
                Protocol = ApplicationProtocol.HTTP
            });
            // not sure if this is necessary...
            lbListener.Connections.AllowDefaultPortFromAnyIpv4("Public access to port 80");

            lbListener.AddTargets("ASGTargets", new AddApplicationTargetsProps
            {
                Port = 5000, // the port the Kestrel-hosted app will be exposed on
                Protocol = ApplicationProtocol.HTTP,
                Targets = new [] { scalingGroup }
            });

            // Can only be set after the group has been attached to a load balancer
            scalingGroup.ScaleOnRequestCount("DemoLoad", new RequestCountScalingProps
            {
                TargetRequestsPerMinute = 10 // enough for demo purposes
            });

            // Emit the url to the load balancer fronting the application fleet
            new CfnOutput(this, "AppUrl", new CfnOutputProps
            {
                Value = $"http://{loadBalancer.LoadBalancerDnsName}"
            });

            #endregion

            #region Application Deployment Pipeline Resources

            var codeDeployApp = new ServerApplication(this, "GigsNearMe", new ServerApplicationProps
            {
                ApplicationName = "GigsNearMe"
            });

            var deploymentGroup = new ServerDeploymentGroup(this, "WebHostDeploymentGroup", new ServerDeploymentGroupProps
            {
                Application = codeDeployApp,
                AutoScalingGroups = new AutoScalingGroup[] { scalingGroup },
                DeploymentGroupName = "GigsNearMeDeploymentGroup",
                InstallAgent = false, // we did this already as part of EC2 instance intitialization userdata
                Role = new Role(this, "GigsNearMeDeploymentRole", new RoleProps
                {
                    AssumedBy = new ServicePrincipal("codedeploy.amazonaws.com"),
                    ManagedPolicies = new IManagedPolicy[]
                    {
                        ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSCodeDeployRole")
                    }
                }),
                DeploymentConfig = ServerDeploymentConfig.ONE_AT_A_TIME
            });

            // create a bucket to hold the initial built application bundles (if using S3-based
            // deployment), and artifacts as they move down the pipeline
            var artifactsBucket = new Bucket(this, "ArtifactsBucket", new BucketProps
            {
                Versioned = true,
                RemovalPolicy = RemovalPolicy.DESTROY,
                AutoDeleteObjects = true
            });

            new CfnOutput(this, "ArtifactsBucketName", new CfnOutputProps
            {
                Value = artifactsBucket.BucketName
            });
            new CfnOutput(this, "DeploymentArtifactName", new CfnOutputProps
            {
                Value = DeploymentArtifactName
            });

            #region S3-based pipeline

            // grant the EC2 hosts access to the bucket, so the CodeDeploy agent can
            // download the deployment bundles
            artifactsBucket.GrantRead(appRole);

            var trail = new Trail(this, "GigsNearMeTrail", new TrailProps
            {
                // this bucket, used only for S3-based deployment, holds the CloudTrail logs
                // which trigger the pipeline to start when a new artifact is uploaded
                Bucket = new Bucket(this, "TrailLogsBucket", new BucketProps
                {
                    RemovalPolicy = RemovalPolicy.DESTROY,
                    AutoDeleteObjects = true
                })
            });

            trail.AddS3EventSelector(
                new []
                {
                    new S3EventSelector
                    {
                        Bucket = artifactsBucket
                    }
                },
                new AddEventSelectorOptions
                {
                    ReadWriteType = ReadWriteType.WRITE_ONLY
                }
            );

            #endregion

            #region GitHub-based pipeline
            /*
            var githubSourceArtifact = new Artifact_("GitHubSourceArtifact");

            var build = new PipelineProject(this, "CodeBuild", new PipelineProjectProps
            {
                // relative path to sample app's file (single html page for now) - this is
                // relative to the repo root, btw
                BuildSpec = BuildSpec.FromSourceFilename(DOTNET-BUILDSPEC-RELPATH-HERE),
                Environment = new BuildEnvironment
                {
                    BuildImage = LinuxBuildImage.AMAZON_LINUX_2_2
                },
            });
            */
            #endregion

            var deploymentArtifact = new Artifact_("DeploymentArtifact");

            var pipeline = new Pipeline(this, "CiCdPipeline", new PipelineProps
            {
                ArtifactBucket = artifactsBucket,
                Stages = new []
                {
                    #region GitHub-based pipeline
                    /*
                    new Amazon.CDK.AWS.CodePipeline.StageProps
                    {
                        StageName = "Source",
                        Actions = new IAction[]
                        {
                            new GitHubSourceAction(new GitHubSourceActionProps
                            {
                                ActionName = "GitHubSource",
                                Repo = this.Node.TryGetContext("repo-name").ToString(),
                                Owner = this.Node.TryGetContext("repo-owner").ToString(),
                                Branch = this.Node.TryGetContext("repo-branch").ToString(),
                                OauthToken = SecretValue.SecretsManager("github-token"),
                                Output = githubSourceArtifact
                            })
                        }
                    },

                    new Amazon.CDK.AWS.CodePipeline.StageProps
                    {
                        StageName = "Build",
                        Actions = new IAction[]
                        {
                            new CodeBuildAction(new CodeBuildActionProps
                            {
                                ActionName = "Build-app",
                                Project = build,
                                Input = githubSourceArtifact,
                                Outputs = new Artifact_[] { deploymentArtifact },
                                RunOrder = 1
                            })
                        }
                    },
                    */
                    #endregion

                    #region S3-based pipeline

                    new Amazon.CDK.AWS.CodePipeline.StageProps
                    {
                        StageName = "BundleUpload",
                        Actions = new []
                        {
                            new S3SourceAction(new S3SourceActionProps
                            {
                                ActionName = "BundleUpload",
                                RunOrder = 1,
                                Bucket = artifactsBucket,
                                BucketKey = DeploymentArtifactName,
                                Trigger = S3Trigger.EVENTS,
                                Output = deploymentArtifact
                            })
                        }
                    },

                    #endregion

                    new Amazon.CDK.AWS.CodePipeline.StageProps
                    {
                        StageName = "BundleDeployment",
                        Actions = new []
                        {
                            new CodeDeployServerDeployAction(new CodeDeployServerDeployActionProps
                            {
                                ActionName = "DeployViaCodeDeploy",
                                RunOrder = 2,
                                DeploymentGroup = deploymentGroup,
                                Input = deploymentArtifact
                            })
                        }
                    }
                }
            });

            #endregion
        }
    }
}
