#pragma checksum "C:\Users\lfili\source\repos\AWS\GigsNearMeAppStart\Views\Home\Index.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "a1d6d3482d73d1bf6fc21c8f9068f6b1b4a15ed7"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCore.Views_Home_Index), @"mvc.1.0.view", @"/Views/Home/Index.cshtml")]
namespace AspNetCore
{
    #line hidden
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
#nullable restore
#line 1 "C:\Users\lfili\source\repos\AWS\GigsNearMeAppStart\Views\_ViewImports.cshtml"
using GigsNearMe;

#line default
#line hidden
#nullable disable
#nullable restore
#line 2 "C:\Users\lfili\source\repos\AWS\GigsNearMeAppStart\Views\_ViewImports.cshtml"
using GigsNearMe.ViewModels;

#line default
#line hidden
#nullable disable
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"a1d6d3482d73d1bf6fc21c8f9068f6b1b4a15ed7", @"/Views/Home/Index.cshtml")]
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"8d324de9221b8d1e7c8f9eeebfd002ff5330a421", @"/Views/_ViewImports.cshtml")]
    #nullable restore
    public class Views_Home_Index : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<IEnumerable<ArtistViewModel>>
    #nullable disable
    {
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            WriteLiteral("\n");
#nullable restore
#line 3 "C:\Users\lfili\source\repos\AWS\GigsNearMeAppStart\Views\Home\Index.cshtml"
  
    ViewBag.Title = "GigsNearMe! Home";

#line default
#line hidden
#nullable disable
            WriteLiteral("\n<h3>Upcoming Tours!</h3>\n\n<div class=\"upcoming-tours-list\">\n\n");
#nullable restore
#line 11 "C:\Users\lfili\source\repos\AWS\GigsNearMeAppStart\Views\Home\Index.cshtml"
     foreach (var item in Model)
    {

#line default
#line hidden
#nullable disable
            WriteLiteral("        <div class=\"row offset-sm-1 justify-content-start align-items-start\">\n\n            <div class=\"col-3\">\n                <h4>");
#nullable restore
#line 16 "C:\Users\lfili\source\repos\AWS\GigsNearMeAppStart\Views\Home\Index.cshtml"
               Write(item.Name);

#line default
#line hidden
#nullable disable
            WriteLiteral("</h4>\n            </div>\n\n            <div class=\"col-9\">\n                <div class=\"card-deck\">\n");
#nullable restore
#line 21 "C:\Users\lfili\source\repos\AWS\GigsNearMeAppStart\Views\Home\Index.cshtml"
                     foreach (var tour in item.Tours)
                    {

#line default
#line hidden
#nullable disable
            WriteLiteral("                    <div class=\"col-sm-6\">\n                        <div class=\"card bg-light mb-3\">\n                            <div class=\"card-body\">\n                                <h5 class=\"card-title\">");
#nullable restore
#line 26 "C:\Users\lfili\source\repos\AWS\GigsNearMeAppStart\Views\Home\Index.cshtml"
                                                  Write(tour.Name);

#line default
#line hidden
#nullable disable
            WriteLiteral("</h5>\n                                <p class=\"card-text\">Starts ");
#nullable restore
#line 27 "C:\Users\lfili\source\repos\AWS\GigsNearMeAppStart\Views\Home\Index.cshtml"
                                                       Write(tour.Start.ToString("dddd MMMM dd, yyyy"));

#line default
#line hidden
#nullable disable
            WriteLiteral(".</p>\n                                ");
#nullable restore
#line 28 "C:\Users\lfili\source\repos\AWS\GigsNearMeAppStart\Views\Home\Index.cshtml"
                           Write(Html.ActionLink("Find out more...", "Details", "Tours", new { id = tour.TourID }, null));

#line default
#line hidden
#nullable disable
            WriteLiteral("\n                            </div>\n                        </div>\n                    </div>\n");
#nullable restore
#line 32 "C:\Users\lfili\source\repos\AWS\GigsNearMeAppStart\Views\Home\Index.cshtml"
                    }

#line default
#line hidden
#nullable disable
            WriteLiteral("                </div>\n            </div>\n        </div>\n");
#nullable restore
#line 36 "C:\Users\lfili\source\repos\AWS\GigsNearMeAppStart\Views\Home\Index.cshtml"
    }

#line default
#line hidden
#nullable disable
            WriteLiteral("</div>\n");
        }
        #pragma warning restore 1998
        #nullable restore
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; } = default!;
        #nullable disable
        #nullable restore
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; } = default!;
        #nullable disable
        #nullable restore
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; } = default!;
        #nullable disable
        #nullable restore
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; } = default!;
        #nullable disable
        #nullable restore
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<IEnumerable<ArtistViewModel>> Html { get; private set; } = default!;
        #nullable disable
    }
}
#pragma warning restore 1591
