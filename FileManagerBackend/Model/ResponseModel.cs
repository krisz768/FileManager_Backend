namespace FileManagerBackend.Model
{
    public class ResponseModel
    {
        public bool Error { get; set; }
        public object Data { get; set; }

        public ResponseModel(bool Error, object Data)
        {
            this.Error = Error;
            this.Data = Data;
        }
    }
}
