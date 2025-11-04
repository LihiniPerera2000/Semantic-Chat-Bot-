//namespace SemanticBotStar.Services
//{
//    public class VectorDbService
//    {
//    }
//}

namespace SemanticBotStar.Services;

public interface IVectorStoreService
{
    Task AddVector(float[] vector, string text, string source);
    Task<List<(float[] vector, string text, string source)>> QueryTopK(float[] queryVector, int k = 3);
}

// Implement with production-ready vector DB client
public class VectorDbService : IVectorStoreService
{
    private readonly List<(float[] vector, string text, string source)> _store = new();

    public Task AddVector(float[] vector, string text, string source)
    {
        _store.Add((vector, text, source));
        return Task.CompletedTask;
    }

    public Task<List<(float[] vector, string text, string source)>> QueryTopK(float[] queryVector, int k = 3)
    {
        var result = _store
            .Select(x => (x.vector, x.text, x.source, sim: CosineSim(x.vector, queryVector)))
            .OrderByDescending(x => x.sim)
            .Take(k)
            .Select(x => (x.vector, x.text, x.source))
            .ToList();
        return Task.FromResult(result);
    }

    private static double CosineSim(float[] a, float[] b)
    {
        double dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
