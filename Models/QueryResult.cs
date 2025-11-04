//namespace SemanticBotStar.Models
//{
//    public class QueryResult
//    {
//    }
//}

namespace SemanticBotStar.Models;

public record QueryResult(string Answer, IEnumerable<string> Context);