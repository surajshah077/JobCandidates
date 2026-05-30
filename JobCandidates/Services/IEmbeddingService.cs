// JobCandidates/Services/IEmbeddingService.cs
namespace JobCandidates.Services
{
    public interface IEmbeddingService
    {
        /// <summary>
        /// Returns an embedding vector for the given text.
        /// </summary>
        Task<double[]> GetEmbeddingAsync(string text);
    }
}