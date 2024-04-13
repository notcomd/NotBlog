namespace Notcomd.Net.Reasult
{
    public class Reasults
    {
        public bool Souress { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; } = null!;
        public Dictionary<string, object> Data { get; set; }

        private Reasults()
        {
            Data = new Dictionary<string, object>();
        }
        public static Reasults Ok()
        {
            var Rt = new Reasults
            {
                Souress = true,
                Message = IStatuCode.GetOk,
                StatusCode = IStatuCode.ReasultOK
            };
            return Rt;
        }

        public static Task<Reasults> Error()
        {
            var Rt = new Reasults
            {
                Souress = false,
                Message = IStatuCode.GetError,
                StatusCode = IStatuCode.ReasultError
            };
            return Task.FromResult(Rt);
        }

        public Reasults SetData(string key, object valua)
        {
            Data.Add(key, valua);
            return this;
        }

        public Reasults SetData(Dictionary<string, object> data)
        {
            Data = data;
            return this;
        }
    }
}
