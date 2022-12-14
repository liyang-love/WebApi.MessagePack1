using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace WebApi.MessagePack.App_Start
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class WebApiTrackerAttribute : ActionFilterAttribute//, ExceptionFilterAttribute  
    {
        private readonly string Key = "_thisWebApiOnActionMonitorLog_";
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            base.OnActionExecuting(actionContext);
            WebApiMonitorLog MonLog = new WebApiMonitorLog();
            MonLog.ExecuteStartTime = DateTime.Now;
            //获取Action 参数
            MonLog.ActionParams = actionContext.ActionArguments;
            MonLog.HttpRequestHeaders = actionContext.Request.Headers.ToString();
            MonLog.HttpMethod = actionContext.Request.Method.Method;

            actionContext.Request.Properties[Key] = MonLog;
            //var form = System.Web.HttpContext.Current.Request.Form;
            #region 如果参数是实体对象，获取序列化后的数据
            Stream stream = actionContext.Request.Content.ReadAsStreamAsync().Result;
            Encoding encoding = Encoding.UTF8;
            stream.Position = 0;
            string responseData = "";
            using (StreamReader reader = new StreamReader(stream, encoding))
            {
                responseData = reader.ReadToEnd().ToString();
            }
            if (!string.IsNullOrWhiteSpace(responseData) && !MonLog.ActionParams.ContainsKey("__EntityParamsList__"))
            {
                MonLog.ActionParams["__EntityParamsList__"] = responseData;
            }
            #endregion
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            WebApiMonitorLog MonLog = actionExecutedContext.Request.Properties[Key] as WebApiMonitorLog;
            MonLog.ExecuteEndTime = DateTime.Now;
            MonLog.ActionName = actionExecutedContext.ActionContext.ActionDescriptor.ActionName;
            MonLog.ControllerName = actionExecutedContext.ActionContext.ActionDescriptor.ControllerDescriptor.ControllerName;
            Debug.WriteLine(MonLog.GetLoginfo());
            if (actionExecutedContext.Exception != null)
            {
                string Msg = string.Format(@"
                请求【{0}Controller】的【{1}】产生异常：
                Action参数：{2}
               Http请求头:{3}
                客户端IP：{4},
                HttpMethod:{5}
                    ", MonLog.ControllerName, MonLog.ActionName, MonLog.GetCollections(MonLog.ActionParams), MonLog.HttpRequestHeaders, "", MonLog.HttpMethod);
                Debug.WriteLine(Msg, actionExecutedContext.Exception);
            }
        }

    }
}
