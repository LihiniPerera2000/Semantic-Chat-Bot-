////------------------------------------------------------------------------------------------------------------------

//using RSemanticBotStar.Services;
//using SemanticBotStar.Models;
//using SemanticBotStar.Services;

////using RAGPrototype.Models;
////using RAGPrototype.Services;
//using Microsoft.AspNetCore.Mvc;
//using DotNetEnv;


////
//// 1?? Load environment variables
////
//Env.Load();

//var builder = WebApplication.CreateBuilder(args);
//builder.Logging.AddConsole();


//// 2?? Register services
//// Add services to the container.
//builder.Services.AddRazorPages();
//builder.Services.AddSingleton<IEmbeddingService, HuggingFaceEmbeddingService>();
//builder.Services.AddSingleton<IChatService, ChatService>();
//builder.Services.AddSingleton<IVectorStoreService, VectorDbService>();
//builder.Services.AddSingleton<IDocumentService, DocumentService>();

//var app = builder.Build();



////
//// 3?? Enable static web UI (index.html in wwwroot)
////
//app.UseDefaultFiles();
//app.UseStaticFiles();

////
//// 4?? Ingest folder endpoint
////
//app.MapPost("/ingest-folder", async ([FromBody] FolderRequest req, IDocumentService docService) =>
//{
//    if (string.IsNullOrWhiteSpace(req.FolderPath))
//        return Results.BadRequest(new { error = "FolderPath is required." });

//    await docService.IngestFolderAsync(req.FolderPath);
//    return Results.Ok(new { message = $"? Folder '{req.FolderPath}' ingestion started." });
//});

////
//// 5?? Query endpoint — performs full RAG (retrieve + generate)
////
//app.MapPost("/query", async (
//    [FromBody] QueryRequest req,
//    IEmbeddingService embedSvc,
//    IVectorStoreService vecStore,
//    IChatService chatSvc) =>
//{
//    if (string.IsNullOrWhiteSpace(req.Question))
//        return Results.BadRequest(new { error = "Question is required." });

//    // Step 1: Embed the question
//    var queryVec = await embedSvc.GetEmbedding(req.Question);
//    if (queryVec.Length == 0)
//        return Results.Problem("Embedding generation failed.");

//    // Step 2: Retrieve top-K similar context chunks
//    var topK = req.TopK <= 0 ? 3 : req.TopK;
//    var topDocs = await vecStore.QueryTopK(queryVec, topK);
//    var context = string.Join("\n\n", topDocs.Select(x => x.text));

//    // Step 3: Combine context + user query for LLM
//    var prompt = $"""
//    You are a helpful assistant. Use the provided context to answer the question accurately.
//    If the context is not relevant, say "I don’t have enough information."

//    Context:
//    {context}

//    Question:
//    {req.Question}
//    """;

//    var answer = await chatSvc.QueryLLM(prompt);

//    return Results.Ok(new QueryResult(
//        Answer: answer,
//        Context: topDocs.Select(d => d.source)
//    ));
//});

//// Configure the HTTP request pipeline.
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Error");
//    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//    app.UseHsts();
//}

////
//// 6?? Run app
////

//app.UseHttpsRedirection();
//app.UseStaticFiles();

//app.UseRouting();

//app.UseAuthorization();

//app.MapRazorPages();

//app.Run();
//===================================================================

using DotNetEnv;
using Microsoft.AspNetCore.Mvc;
using RSemanticBotStar.Services;
using SemanticBotStar.Models;
using SemanticBotStar.Services;

//
// 1?? Load environment variables
//
Env.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole();

//
// 2?? Register Razor Pages + Services
//
builder.Services.AddRazorPages();  // <-- Add Razor Pages support
builder.Services.AddSingleton<IEmbeddingService, HuggingFaceEmbeddingService>();
builder.Services.AddSingleton<IChatService, ChatService>();
builder.Services.AddSingleton<IVectorStoreService, VectorDbService>();
builder.Services.AddSingleton<IDocumentService, DocumentService>();

var app = builder.Build();

//
// 3?? Middleware
//
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

//
// 4?? API endpoints
//

// Endpoint to ingest folder
app.MapPost("/ingest-folder", async ([FromBody] FolderRequest req, IDocumentService docService) =>
{
    if (string.IsNullOrWhiteSpace(req.FolderPath))
        return Results.BadRequest(new { error = "FolderPath is required." });

    await docService.IngestFolderAsync(req.FolderPath);
    return Results.Ok(new { message = $"? Folder '{req.FolderPath}' ingestion started." });
});

// Endpoint to query knowledge base
app.MapPost("/query", async (
    [FromBody] QueryRequest req,
    IEmbeddingService embedSvc,
    IVectorStoreService vecStore,
    IChatService chatSvc) =>
{
    if (string.IsNullOrWhiteSpace(req.Question))
        return Results.BadRequest(new { error = "Question is required." });

    var queryVec = await embedSvc.GetEmbedding(req.Question);
    if (queryVec.Length == 0)
        return Results.Problem("Embedding generation failed.");

    var topK = req.TopK <= 0 ? 3 : req.TopK;
    var topDocs = await vecStore.QueryTopK(queryVec, topK);
    var context = string.Join("\n\n", topDocs.Select(x => x.text));

    var prompt = $"""
    You are a helpful assistant. Use the provided context to answer the question accurately.
    If the context is not relevant, say "I don’t have enough information."

    Context:
    {context}

    Question:
    {req.Question}
    """;

    var answer = await chatSvc.QueryLLM(prompt);

    return Results.Ok(new QueryResult(
        Answer: answer,
        Context: topDocs.Select(d => d.source)
    ));
});

//
// 5?? Run app
//
app.Run();
