//namespace SemanticBotStar.Models
//{
//    public class QueryRequest
//    {
//    }
//}

namespace SemanticBotStar.Models;

public record QueryRequest(string Question, int TopK = 3);