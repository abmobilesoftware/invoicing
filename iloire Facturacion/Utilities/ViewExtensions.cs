using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace iloire_Facturacion.Utilities
{
   public static class ViewExtensionMethods
 {

      public static MvcHtmlString ImageLink(this UrlHelper helper, string imageUrl, string altText, string actionName, string controllerName, string title, object routeValues, string imgID = null)
      {
         var imgTag = new TagBuilder("img");
         imgTag.MergeAttribute("src", imageUrl);
         imgTag.MergeAttribute("alt", altText);
         imgTag.MergeAttribute("title", title);
         if (!String.IsNullOrEmpty(imgID))
         {
            imgTag.GenerateId(imgID);
         }

         var link = helper.Action(actionName, controllerName);
         TagBuilder imglink = new TagBuilder("a");
         imglink.MergeAttribute("href", link.ToString());
         imglink.InnerHtml = imgTag.ToString();
         return new MvcHtmlString(imglink.ToString()); ;
      }
      public static MvcHtmlString ImageLink(this HtmlHelper htmlHelper, string imgSrc, string cultureName, object htmlAttributes, object imgHtmlAttributes, string languageRouteName = "lang", bool strictSelected = false)
      {
         UrlHelper urlHelper = ((Controller)htmlHelper.ViewContext.Controller).Url;
         TagBuilder imgTag = new TagBuilder("img");
         imgTag.MergeAttribute("src", imgSrc);
         imgTag.MergeAttributes((IDictionary<string, string>)imgHtmlAttributes, true);

         var language = htmlHelper.LanguageUrl(cultureName, languageRouteName, strictSelected);
         string url = language.Url;

         TagBuilder imglink = new TagBuilder("a");
         imglink.MergeAttribute("href", url);
         imglink.InnerHtml = imgTag.ToString();
         imglink.MergeAttributes((IDictionary<string, string>)htmlAttributes, true);

         //if the current page already contains the language parameter make sure the corresponding html element is marked
         string currentLanguage = htmlHelper.ViewContext.RouteData.GetRequiredString("lang");
         if (cultureName.Equals(currentLanguage, StringComparison.InvariantCultureIgnoreCase))
         {
            imglink.AddCssClass("selectedLanguage");
         }
         return new MvcHtmlString(imglink.ToString());
      }

   
 }
}