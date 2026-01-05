using Wolverine.Http;

namespace Test
{
    public class HelloEndpoint
    {
        [WolverineGet("/")]
        public string Get() => "Hello.";
    }
}
