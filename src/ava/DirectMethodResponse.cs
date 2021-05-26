using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
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
        public int ResponseCode { get; private set; }

        public string ResponseBodyString {
            get; private set;
        }

        public bool IsSuccess 
        {
            get
            {
                return ResponseCode <= 204;
            }
        }

        public dynamic ResponseBody {
            get; private set;
        }

        public DirectMethodResponse() { }

        public DirectMethodResponse(int code, string body)
        {
            this.ResponseCode = code;

            this.ResponseBodyString = body;

            dynamic responseBody = null;
            try
            {
                responseBody = JsonConvert.DeserializeObject<ExpandoObject>(ResponseBodyString);

                this.ResponseBody = responseBody;

                ResponseBodyString = JsonConvert.SerializeObject(responseBody, Formatting.Indented);


            }
            catch { }
        }

        public override string ToString()
        {
            return $"## {ResponseCode}{Environment.NewLine}{ResponseBodyString}";
        }
    }
}
