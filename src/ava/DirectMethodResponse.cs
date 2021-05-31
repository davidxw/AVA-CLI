using Newtonsoft.Json;
using System;
using System.Dynamic;

namespace ava
{
    public class DirectMethodResponse
    {
        public int ResponseCode { get; private set; }

        public string ResponseBodyString {
            get; private set;
        }

        public bool IsSuccess => ResponseCode <= 204;

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

        public override string ToString() => $"## {ResponseCode}{Environment.NewLine}{ResponseBodyString}";

    }
}
