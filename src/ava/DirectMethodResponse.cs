using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ava
{
    public class DirectMethodResponse
    {
        public int ResponseCode { get; set; }

        public string ResponseBodyString {
            get
            {
                return JsonConvert.SerializeObject(ResponseBody, Formatting.Indented);
            }
        }

        public string ResponseMessage { get; set; }

        public bool IsSuccess 
        {
            get
            {
                return ResponseCode <= 204;
            }
        }

        public dynamic ResponseBody { get; set; }

        public DirectMethodResponse() { }

        public DirectMethodResponse(int code, string body)
        {
            this.ResponseCode = code;
            this.ResponseBody = JsonConvert.DeserializeObject<ExpandoObject>(body);
        }

        public override string ToString()
        {
            return $"## {ResponseCode}{Environment.NewLine}{ResponseBodyString}";
        }
    }
}
