using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CheckIn.API.Helpers
{
    public class Functions
    {
        public static void AddCountHeader(int totalCount)
        {
            HttpContext.Current.Response.Headers.Add("X-Total-Count", totalCount.ToString());
        }
    }
}