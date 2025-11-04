//using SemanticBotStar.Services;
//using SemanticBotStar.Utils;

//namespace SemanticBotStar.Services
//{
//    public class DocumentService
//    {
//    }
//}

using Microsoft.Extensions.Logging;
using SemanticBotStar.Utils;

namespace SemanticBotStar.Services;

public interface IDocumentService
{
    Task IngestFolderAsync(string folderPath);
}

public class DocumentService : IDocumentService
{
    private readonly IEmbeddingService _embedding;
    private readonly IVectorStoreService _vectorStore;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(IEmbeddingService embedding, IVectorStoreService vectorStore, ILogger<DocumentService> logger)
    {
        _embedding = embedding;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    public async Task IngestFolderAsync(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            _logger.LogWarning("Folder not found: {0}", folderPath);
            return;
        }

        var files = Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".txt") || f.EndsWith(".md") || f.EndsWith(".pdf") || f.EndsWith(".docx"))
            .ToList();

        // Limit how many files are processed simultaneously
        var semaphore = new SemaphoreSlim(1); // ← 2 at a time

        var tasks = files.Select(async file =>
        {
            await semaphore.WaitAsync();
            try
            {
                var text = FileParser.ParseFile(file);
                var chunks = FileParser.ChunkText(text, 500);

                _logger.LogInformation("File {File} produced {Count} chunks.", file, chunks.Count());



                // Optional: limit chunk concurrency too
                foreach (var chunk in chunks)
                {
                    try
                    {
                        var vec = await _embedding.GetEmbedding(chunk);
                        if (vec.Length == 0)
                        {
                            _logger.LogWarning($"Embedding for chunk from {file} returned empty.");
                            continue;
                        }

                        await _vectorStore.AddVector(vec, chunk, Path.GetFileName(file));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Embedding failed for file {file}.");
                    }
                }


                _logger.LogInformation("Ingested {0}", Path.GetFileName(file));
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        _logger.LogInformation("Folder ingestion complete.");
    }
}
