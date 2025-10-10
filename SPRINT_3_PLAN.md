# Sprint 3 Plan - Embedding Generation & Semantic Search
**YouTube RAG .NET Project**

**Document Version:** 1.0
**Sprint Number:** Sprint 3
**Sprint Start:** October 10, 2025
**Sprint End:** October 17, 2025
**Sprint Duration:** 5 working days
**Product Owner:** Senior Product Owner
**Technical Lead:** Software Architect

---

## Executive Summary

Sprint 3 represents the **final core feature delivery** for the YouTube RAG MVP, completing the semantic search capabilities. With Sprint 2's successful completion of video ingestion and transcription (41 story points in 2 days), Sprint 3 focuses on **replacing mock embeddings with real semantic vectors** and **implementing vector similarity search**.

### Sprint Goal

**"Enable semantic search over transcribed video content by generating real embeddings and implementing vector similarity search with sub-second query performance."**

### Key Outcomes Expected

1. **Real Embedding Generation**: Replace mock embeddings with actual semantic vectors using ONNX Runtime
2. **Vector Storage**: Implement efficient vector storage and indexing (pgvector or specialized)
3. **Semantic Search**: Enable similarity search with ranking and filtering
4. **Query API**: Expose search endpoints with context retrieval
5. **RAG Preparation**: Structure for LLM context injection

### Timeline

- **Start Date:** October 10, 2025 (Thursday)
- **End Date:** October 17, 2025 (Thursday)
- **Duration:** 5 working days
- **Sprint Review:** October 17, 3:00 PM
- **Sprint Retrospective:** October 17, 4:00 PM

---

## Sprint Velocity & Capacity

### Sprint 2 Performance (Baseline)

| Metric | Value | Notes |
|--------|-------|-------|
| **Story Points Delivered** | 41 | 100% commitment |
| **Actual Duration** | 2 days | vs 10 days planned |
| **Velocity** | 20.5 pts/day | Exceptional with agent-driven dev |
| **Quality** | A+ (97%) | 0 errors, Clean Architecture perfect |
| **Test Coverage** | 62.3% | Core paths covered |

### Sprint 3 Capacity Planning

**Assumptions:**
- Agent-driven development continues
- No major blockers or external dependencies
- Leveraging learnings from Sprint 2
- Conservative estimates given embedding complexity

**Planned Capacity:**
- **Working Days:** 5 days
- **Estimated Velocity:** 15-18 pts/day (conservative)
- **Total Capacity:** 75-90 story points
- **Committed:** 48 story points (53% buffer)
- **Buffer for testing/issues:** 27-42 pts

---

## Epic Breakdown

### Epic 6: Embedding Generation (23 pts)

**Goal:** Generate real semantic embeddings for all transcript segments using ONNX Runtime with sentence-transformers model

**Business Value:** Enables semantic search, the core differentiator of RAG systems
**Priority:** MUST HAVE (P0)
**RICE Score:** 180.0

#### User Stories:
- YRUS-0601: Configure ONNX Embedding Model (5 pts)
- YRUS-0602: Generate Embeddings for Segments (8 pts)
- YRUS-0603: Store Embeddings in Vector-Capable Storage (5 pts)
- YRUS-0604: Batch Embedding Processing Job (5 pts)

---

### Epic 7: Search & Query (17 pts)

**Goal:** Implement semantic search over embeddings with ranking and filtering

**Business Value:** Core MVP feature - enables users to search video content semantically
**Priority:** MUST HAVE (P0)
**RICE Score:** 165.0

#### User Stories:
- YRUS-0701: Vector Similarity Search (5 pts)
- YRUS-0702: Query API with Ranking (5 pts)
- YRUS-0703: Context Retrieval for RAG (5 pts)
- YRUS-0704: Search Result Optimization (2 pts)

---

### Epic 8: Integration & Quality (8 pts)

**Goal:** End-to-end integration testing and quality assurance

**Business Value:** Ensures production readiness and reliability
**Priority:** MUST HAVE (P0)

#### User Stories:
- YRUS-0801: End-to-End Search Testing (5 pts)
- YRUS-0802: Performance Benchmarking (3 pts)

---

## Total Story Points: 48 pts

**Distribution:**
- Epic 6 (Embeddings): 23 pts (48%)
- Epic 7 (Search): 17 pts (35%)
- Epic 8 (Quality): 8 pts (17%)

---

## Detailed User Stories

---

## YRUS-0601: Configure ONNX Embedding Model

**Story Points:** 5
**Priority:** Critical (P0)
**Sprint:** 3
**Epic:** Embedding Generation
**Git Branch:** `YRUS-0601_configure_onnx_embedding`

### User Story

**As a** system administrator
**I want** ONNX Runtime configured with a sentence-transformer model
**So that** real semantic embeddings can be generated for transcript segments

### Acceptance Criteria

**AC1: ONNX Runtime Setup**
- Given the application starts
- When initializing embedding services
- Then ONNX Runtime is loaded and configured
- And the sentence-transformer model (all-MiniLM-L6-v2) is available
- And model loading completes in <5 seconds on first use
- And subsequent uses are from cached model

**AC2: Model Management**
- Given the embedding model is required
- When checking model availability
- Then system verifies model exists in configured path: `./models/embeddings/all-MiniLM-L6-v2.onnx`
- And downloads model if missing (auto-download from Hugging Face)
- And validates model integrity via checksum
- And logs model version and configuration

**AC3: Configuration Options**
- Given embedding configuration
- When system is configured
- Then support model path configuration via appsettings.json
- And support embedding dimension configuration (default: 384)
- And support batch size configuration (default: 32)
- And support GPU acceleration if available (fallback to CPU)

**AC4: Embedding Service Interface**
- Given embedding service implementation
- When interface is defined
- Then provide `IEmbeddingService.GenerateEmbeddingAsync(string text, CancellationToken)`
- And provide `IEmbeddingService.GenerateBatchEmbeddingsAsync(List<string> texts, CancellationToken)`
- And return float arrays of dimension 384
- And support cancellation for long operations

**AC5: Error Handling**
- Given potential failures
- When errors occur
- Then handle model not found with clear error message
- And handle ONNX Runtime initialization failures
- And handle out-of-memory conditions gracefully
- And log detailed error information for debugging

### Technical Notes

**Stack Tecnológico:**
- **ONNX Runtime:** Microsoft.ML.OnnxRuntime (NuGet)
- **Model:** sentence-transformers/all-MiniLM-L6-v2 from Hugging Face
- **Tokenizer:** Basic WordPiece tokenizer (or SentencePiece)
- **Alternative:** Python integration via pythonnet (if ONNX complex)

**Model Specifications:**
```
Model: all-MiniLM-L6-v2
Format: ONNX
Input: Tokenized text (max 256 tokens)
Output: float[384] embedding vector
Size: ~90 MB
Performance: ~2000 embeddings/sec on CPU, ~10000 on GPU
```

**Configuration (appsettings.json):**
```json
{
  "Embedding": {
    "Provider": "ONNX",
    "ModelPath": "./models/embeddings/all-MiniLM-L6-v2.onnx",
    "VocabPath": "./models/embeddings/vocab.txt",
    "Dimension": 384,
    "BatchSize": 32,
    "MaxTokens": 256,
    "UseGPU": false,
    "NormalizeEmbeddings": true,
    "AutoDownload": true,
    "HuggingFaceModelId": "sentence-transformers/all-MiniLM-L6-v2"
  }
}
```

**File Structure:**
```
YoutubeRag.Application/
├── Configuration/
│   └── EmbeddingOptions.cs (CREAR)
├── Interfaces/Services/
│   └── IEmbeddingService.cs (ACTUALIZAR - replace mock)
└── Services/
    └── OnnxEmbeddingService.cs (CREAR)

YoutubeRag.Infrastructure/
└── ML/
    ├── OnnxModelManager.cs (CREAR)
    ├── TokenizerService.cs (CREAR)
    └── VectorNormalizer.cs (CREAR)

./models/embeddings/ (directory)
├── all-MiniLM-L6-v2.onnx
├── vocab.txt
└── tokenizer.json
```

**Tokenization Strategy:**
```csharp
public class TokenizerService
{
    // Tokenize text for ONNX model
    public (long[] InputIds, long[] AttentionMask, long[] TokenTypeIds) Tokenize(string text)
    {
        // 1. Clean and normalize text
        // 2. WordPiece tokenization
        // 3. Add [CLS] and [SEP] tokens
        // 4. Pad/truncate to max_length=256
        // 5. Create attention mask
        // 6. Return as ONNX input tensors
    }
}
```

**ONNX Inference:**
```csharp
public class OnnxEmbeddingService : IEmbeddingService
{
    private readonly InferenceSession _session;
    private readonly TokenizerService _tokenizer;

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct)
    {
        // 1. Tokenize input
        var (inputIds, attentionMask, tokenTypeIds) = _tokenizer.Tokenize(text);

        // 2. Create ONNX tensors
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputIds),
            NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask),
            NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIds)
        };

        // 3. Run inference
        using var results = _session.Run(inputs);

        // 4. Extract embedding from output
        var embedding = results.First().AsEnumerable<float>().ToArray();

        // 5. Normalize (L2 norm)
        return NormalizeVector(embedding);
    }
}
```

**Alternative: Python Integration (Fallback)**
If ONNX proves complex, use Python via `pythonnet`:
```csharp
public class PythonEmbeddingService : IEmbeddingService
{
    dynamic _model;

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct)
    {
        using (Py.GIL())
        {
            dynamic embedding = _model.encode(text);
            return embedding.tolist();
        }
    }
}
```

### Definition of Done

- [ ] **Código Implementado**
  - [ ] `EmbeddingOptions` configuration class
  - [ ] `OnnxEmbeddingService` implementation
  - [ ] `OnnxModelManager` for model lifecycle
  - [ ] `TokenizerService` for text preprocessing
  - [ ] Model auto-download logic
  - [ ] GPU detection and fallback to CPU
  - [ ] Code review completed

- [ ] **Testing**
  - [ ] Unit tests for tokenization logic
  - [ ] Unit tests for embedding generation
  - [ ] Integration test with real model
  - [ ] Performance test: >1000 embeddings/sec on CPU
  - [ ] Manual testing with various text lengths
  - [ ] Test coverage >85% for embedding services

- [ ] **Build y Deployment**
  - [ ] Build exitoso (0 errors, 0 warnings)
  - [ ] ONNX Runtime NuGet installed
  - [ ] Model downloaded and validated
  - [ ] Configuration documented

- [ ] **Documentación**
  - [ ] XML comments on all public APIs
  - [ ] README with model setup instructions
  - [ ] Troubleshooting guide for ONNX issues
  - [ ] Performance benchmarks documented

- [ ] **Validación Manual (OBLIGATORIA)**
  - [ ] Generate embedding for sample text
  - [ ] Verify embedding dimension (384)
  - [ ] Verify embeddings are normalized
  - [ ] Compare with Python sentence-transformers output
  - [ ] Test batch processing (32+ texts)

### Dependencies

- **Bloqueantes:** None (can run in parallel with Sprint 2 completion)
- **Bloquea a:** YRUS-0602 (needs embedding service ready)

### Story Points Justification

- **Complexity:** High (ONNX integration, tokenization)
- **Effort:** 5-6 hours development + 2-3 hours testing
- **Risk:** Medium (ONNX may have challenges, Python fallback available)
- **Learning Curve:** Medium (ONNX Runtime is well-documented)

**Total: 5 pts**

---

## YRUS-0602: Generate Embeddings for Segments

**Story Points:** 8
**Priority:** Critical (P0)
**Sprint:** 3
**Epic:** Embedding Generation
**Git Branch:** `YRUS-0602_generate_embeddings_segments`

### User Story

**As a** system
**I want** to generate real embeddings for all transcript segments
**So that** semantic search can be performed over video content

### Acceptance Criteria

**AC1: Segment Loading**
- Given transcript segments in database
- When embedding generation job runs
- Then query all segments WHERE `EmbeddingVector IS NULL OR EmbeddingVector LIKE '%mock%'`
- And count total segments to process
- And log segment count per video
- And support resumption if interrupted

**AC2: Batch Processing**
- Given segments to embed
- When processing
- Then batch segments in groups of 32 (configurable)
- And call `IEmbeddingService.GenerateBatchEmbeddingsAsync(batch)`
- And process batches sequentially (for reliability) or in parallel (for speed)
- And track progress per batch
- And update Job progress percentage

**AC3: Embedding Storage**
- Given generated embeddings
- When storing in database
- Then serialize float[384] to JSON: `"[0.123, -0.456, ...]"`
- And update `TranscriptSegment.EmbeddingVector`
- And mark segment as processed
- And use bulk update for performance (batch commit every 100 segments)

**AC4: Progress Tracking**
- Given long-running embedding job
- When processing batches
- Then update Job.Progress every 5% increment
- And emit SignalR progress event
- And log estimated time remaining
- And calculate embeddings/second rate

**AC5: Error Handling & Retry**
- Given potential failures
- When errors occur
- Then retry individual segment on batch failure
- And skip segments that consistently fail (after 3 attempts)
- And log failed segment IDs for manual review
- And continue processing remaining segments
- And mark job as "Partial Success" if >90% succeed

### Technical Notes

**Stack Tecnológico:**
- `IEmbeddingService` from YRUS-0601
- EF Core for database operations
- Hangfire for background job processing
- SignalR for progress notifications

**File Structure:**
```
YoutubeRag.Application/
├── Jobs/
│   └── EmbeddingGenerationJobProcessor.cs (ACTUALIZAR)
└── Services/
    └── EmbeddingGenerationService.cs (CREAR)

YoutubeRag.Infrastructure/
└── Repositories/
    └── TranscriptSegmentRepository.cs (ACTUALIZAR - add bulk update)
```

**Embedding Generation Flow:**
```
1. Load Segments (Progress: 0% → 5%)
   └─ Query segments WHERE EmbeddingVector IS NULL OR LIKE '%mock%'

2. Batch Processing (Progress: 5% → 95%)
   ├─ Split into batches (size: 32)
   ├─ For each batch:
   │   ├─ Extract text from segments
   │   ├─ Call EmbeddingService.GenerateBatchEmbeddingsAsync()
   │   ├─ Serialize embeddings to JSON
   │   ├─ Update TranscriptSegment.EmbeddingVector
   │   ├─ Bulk update to database
   │   └─ Emit progress event
   └─ Handle errors per batch

3. Finalize (Progress: 95% → 100%)
   ├─ Update Video.EmbeddingStatus → Completed
   ├─ Update Video.EmbeddedAt → DateTime.UtcNow
   ├─ Update Job.Status → Completed
   └─ Log summary statistics
```

**Batch Processing Algorithm:**
```csharp
public class EmbeddingGenerationService
{
    public async Task ProcessVideoEmbeddingsAsync(string videoId, CancellationToken ct)
    {
        // 1. Load segments without embeddings
        var segments = await _repository.GetSegmentsWithoutEmbeddingsAsync(videoId);
        var totalSegments = segments.Count;

        // 2. Batch processing
        var batchSize = _options.BatchSize; // Default: 32
        var batches = segments.Chunk(batchSize);

        int processed = 0;
        var failedSegmentIds = new List<string>();

        foreach (var batch in batches)
        {
            try
            {
                // Extract texts
                var texts = batch.Select(s => s.Text).ToList();

                // Generate embeddings (batch inference)
                var embeddings = await _embeddingService.GenerateBatchEmbeddingsAsync(texts, ct);

                // Update segments
                for (int i = 0; i < batch.Length; i++)
                {
                    batch[i].EmbeddingVector = SerializeEmbedding(embeddings[i]);
                }

                // Bulk update
                await _repository.BulkUpdateAsync(batch);

                // Progress
                processed += batch.Length;
                var progress = (int)((processed / (double)totalSegments) * 90) + 5;
                await _progressService.NotifyJobProgressAsync(jobId, progress);
            }
            catch (Exception ex)
            {
                // Retry individual segments
                foreach (var segment in batch)
                {
                    if (!await TryProcessSegmentAsync(segment, ct))
                    {
                        failedSegmentIds.Add(segment.Id);
                    }
                }
            }
        }

        // 3. Finalize
        var successRate = (totalSegments - failedSegmentIds.Count) / (double)totalSegments;
        if (successRate >= 0.9)
        {
            await FinalizeSuccessAsync(videoId);
        }
        else
        {
            await FinalizePartialAsync(videoId, failedSegmentIds);
        }
    }

    private string SerializeEmbedding(float[] embedding)
    {
        return JsonSerializer.Serialize(embedding);
    }
}
```

**Performance Targets:**
```
CPU Performance:
- Batch size: 32
- Throughput: 1000 embeddings/sec
- 10-minute video (~150 segments): ~10 seconds to embed

GPU Performance (if available):
- Batch size: 64
- Throughput: 5000 embeddings/sec
- 10-minute video: ~2 seconds to embed
```

**Database Bulk Update:**
```csharp
public async Task BulkUpdateEmbeddingsAsync(IEnumerable<TranscriptSegment> segments)
{
    // Use raw SQL for performance
    var updates = segments.Select(s => new
    {
        s.Id,
        EmbeddingVector = s.EmbeddingVector,
        UpdatedAt = DateTime.UtcNow
    });

    // Batch update (100 at a time)
    foreach (var batch in updates.Chunk(100))
    {
        await _context.BulkUpdateAsync(batch);
    }
}
```

**Hangfire Job:**
```csharp
[AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
public class EmbeddingGenerationJob
{
    public async Task ExecuteAsync(string videoId)
    {
        await _embeddingGenerationService.ProcessVideoEmbeddingsAsync(videoId, CancellationToken.None);
    }
}
```

### Definition of Done

- [ ] **Código Implementado**
  - [ ] `EmbeddingGenerationService` complete
  - [ ] Batch processing logic
  - [ ] Bulk update implementation
  - [ ] Progress tracking integration
  - [ ] Error handling and retry logic
  - [ ] Hangfire job updated
  - [ ] Code review completed

- [ ] **Testing**
  - [ ] Unit tests for batch processing
  - [ ] Unit tests for serialization
  - [ ] Integration test with real segments
  - [ ] Performance test with 500+ segments
  - [ ] Error scenario testing
  - [ ] Test coverage >80%

- [ ] **Build y Deployment**
  - [ ] Build exitoso
  - [ ] Database bulk operations tested
  - [ ] SignalR progress working

- [ ] **Performance**
  - [ ] Embedding generation: >1000/sec on CPU
  - [ ] Bulk update: <5 seconds for 1000 segments
  - [ ] Memory usage reasonable (<2GB)
  - [ ] No memory leaks after 10+ videos

- [ ] **Documentación**
  - [ ] XML comments complete
  - [ ] Performance benchmarks documented
  - [ ] Error recovery procedures documented

- [ ] **Validación Manual (OBLIGATORIA)**
  - [ ] Process video with 200+ segments
  - [ ] Verify all embeddings stored correctly
  - [ ] Check embedding dimensions (384)
  - [ ] Verify progress tracking works
  - [ ] Test recovery after interruption

### Dependencies

- **Bloqueantes:** YRUS-0601 (requires embedding service)
- **Bloquea a:** YRUS-0603, YRUS-0701 (search needs embeddings)

### Story Points Justification

- **Complexity:** High (batch processing, error handling, performance)
- **Effort:** 8-10 hours development + 3-4 hours testing
- **Risk:** Medium (performance critical, large data volumes)
- **Integration:** High (database, Hangfire, SignalR)

**Total: 8 pts**

---

## YRUS-0603: Store Embeddings in Vector-Capable Storage

**Story Points:** 5
**Priority:** Critical (P0)
**Sprint:** 3
**Epic:** Embedding Generation
**Git Branch:** `YRUS-0603_vector_storage`

### User Story

**As a** system architect
**I want** efficient vector storage and indexing
**So that** similarity search performs in <1 second

### Acceptance Criteria

**AC1: Storage Strategy Selection**
- Given vector storage requirements
- When evaluating options
- Then decide between:
  - **Option A:** MySQL JSON + application-level search (MVP)
  - **Option B:** PostgreSQL + pgvector extension (recommended)
  - **Option C:** Specialized vector DB (Qdrant, Milvus, Pinecone)
- And document decision in ADR (Architecture Decision Record)
- And implement chosen strategy

**AC2: Vector Storage (MySQL JSON - MVP)**
- Given embeddings as float[384]
- When storing
- Then serialize to JSON: `"[0.1, -0.2, ...]"`
- And store in `TranscriptSegment.EmbeddingVector` (TEXT/JSON column)
- And create index on `VideoId` for filtering
- And support efficient bulk updates

**AC3: Vector Storage (pgvector - Recommended)**
- Given PostgreSQL with pgvector extension
- When storing embeddings
- Then use `vector(384)` column type
- And create HNSW or IVFFlat index for similarity search
- And support `<->` (L2 distance) and `<=>` (cosine distance) operators
- And configure index parameters for performance

**AC4: Retrieval Performance**
- Given stored embeddings
- When querying
- Then single segment retrieval <10ms
- And batch retrieval (100 segments) <50ms
- And full video segments <100ms
- And support filtering by VideoId efficiently

**AC5: Migration Strategy**
- Given existing mock embeddings
- When migrating to real embeddings
- Then provide script to identify mock embeddings
- And support incremental replacement
- And verify embedding dimension before storage
- And handle NULL embeddings gracefully

### Technical Notes

**Option A: MySQL JSON (MVP - Fastest to implement)**

**Pros:**
- Already using MySQL
- No new infrastructure
- Simple implementation
- Sufficient for MVP scale

**Cons:**
- Similarity search in application code (slower)
- No native vector indexing
- Limited scalability
- Not optimal for large-scale search

**Implementation:**
```csharp
// Current TranscriptSegment schema
public class TranscriptSegment : BaseEntity
{
    // ... existing fields
    public string? EmbeddingVector { get; set; } // JSON: "[0.1, -0.2, ...]"
}

// Configuration
entity.Property(ts => ts.EmbeddingVector)
    .HasColumnType("text")
    .IsRequired(false);

// Create index for filtering
CREATE INDEX idx_transcriptsegments_videoid ON TranscriptSegments(VideoId);
```

**Similarity Search (Application-Level):**
```csharp
public class InMemorySimilaritySearch : ISimilaritySearchService
{
    public async Task<List<SearchResult>> SearchAsync(float[] queryEmbedding, int topK, string? videoIdFilter)
    {
        // 1. Load segments from database (with optional VideoId filter)
        var segments = await _repository.GetSegmentsWithEmbeddingsAsync(videoIdFilter);

        // 2. Deserialize embeddings
        var segmentEmbeddings = segments.Select(s => new
        {
            Segment = s,
            Embedding = JsonSerializer.Deserialize<float[]>(s.EmbeddingVector)
        }).ToList();

        // 3. Calculate cosine similarity in-memory
        var similarities = segmentEmbeddings.Select(se => new
        {
            se.Segment,
            Similarity = CosineSimilarity(queryEmbedding, se.Embedding)
        }).ToList();

        // 4. Sort and take top-K
        return similarities
            .OrderByDescending(x => x.Similarity)
            .Take(topK)
            .Select(x => new SearchResult
            {
                Segment = x.Segment,
                Score = x.Similarity
            })
            .ToList();
    }

    private double CosineSimilarity(float[] a, float[] b)
    {
        var dotProduct = a.Zip(b, (x, y) => x * y).Sum();
        var magnitudeA = Math.Sqrt(a.Sum(x => x * x));
        var magnitudeB = Math.Sqrt(b.Sum(x => x * x));
        return dotProduct / (magnitudeA * magnitudeB);
    }
}
```

**Pros of In-Memory Search:**
- Simple implementation
- Exact results (no approximation)
- Works with existing MySQL

**Cons:**
- Memory intensive for large datasets
- Slower than indexed search (but acceptable for MVP)
- Scales linearly with segment count

---

**Option B: PostgreSQL + pgvector (Recommended)**

**Pros:**
- Native vector indexing (HNSW, IVFFlat)
- Fast similarity search
- Open-source, free
- Good documentation

**Cons:**
- Requires PostgreSQL migration
- Additional infrastructure
- Learning curve
- Not compatible with current MySQL

**Implementation (if migrating):**
```sql
-- Install pgvector extension
CREATE EXTENSION vector;

-- Alter table to add vector column
ALTER TABLE "TranscriptSegments"
ADD COLUMN "EmbeddingVector" vector(384);

-- Create HNSW index for cosine similarity
CREATE INDEX ON "TranscriptSegments"
USING hnsw ("EmbeddingVector" vector_cosine_ops);

-- Example similarity query
SELECT "Id", "Text", "VideoId",
       1 - ("EmbeddingVector" <=> '[0.1, 0.2, ...]') AS similarity
FROM "TranscriptSegments"
WHERE "VideoId" = 'video-id'
ORDER BY "EmbeddingVector" <=> '[0.1, 0.2, ...]'
LIMIT 10;
```

**C# Integration:**
```csharp
public class PgVectorSimilaritySearch : ISimilaritySearchService
{
    public async Task<List<SearchResult>> SearchAsync(float[] queryEmbedding, int topK, string? videoIdFilter)
    {
        var query = _context.TranscriptSegments
            .Where(ts => videoIdFilter == null || ts.VideoId == videoIdFilter)
            .OrderBy(ts => EF.Functions.CosineDistance(ts.EmbeddingVector, queryEmbedding))
            .Take(topK);

        var results = await query.ToListAsync();

        return results.Select(r => new SearchResult
        {
            Segment = r,
            Score = 1 - CosineDistance(queryEmbedding, r.EmbeddingVector)
        }).ToList();
    }
}
```

---

**Option C: Specialized Vector DB**

**Qdrant:**
- Purpose-built for vectors
- Excellent performance
- Docker deployment
- REST API

**Pinecone:**
- Fully managed
- Great performance
- Costs money
- Vendor lock-in

**Recommendation for MVP:** **Option A (MySQL JSON + In-Memory Search)**

**Rationale:**
1. **Time to Market:** Fastest implementation (0 infrastructure changes)
2. **MVP Scale:** 10-20 videos = 1000-3000 segments (in-memory search is fine)
3. **Simplicity:** No migration, no new infrastructure
4. **Reversible:** Easy to migrate to pgvector or Qdrant later

**Future Migration Path:**
- Sprint 4+: Evaluate pgvector if performance issues
- Production Scale: Consider specialized vector DB

### Definition of Done

- [ ] **Decisión Documentada**
  - [ ] ADR created for storage strategy
  - [ ] Pros/cons analyzed
  - [ ] MVP recommendation documented

- [ ] **Código Implementado**
  - [ ] Storage implementation (MySQL JSON)
  - [ ] Bulk update optimized
  - [ ] Index created on VideoId
  - [ ] In-memory similarity search service
  - [ ] Code review completed

- [ ] **Testing**
  - [ ] Unit tests for storage operations
  - [ ] Unit tests for similarity calculation
  - [ ] Performance test with 1000 segments
  - [ ] Load test with 5000 segments
  - [ ] Test coverage >85%

- [ ] **Build y Deployment**
  - [ ] Build exitoso
  - [ ] Migration script for embedding verification
  - [ ] Index creation automated

- [ ] **Performance**
  - [ ] Single segment retrieval <10ms
  - [ ] Similarity search (100 candidates) <200ms
  - [ ] Full video search <500ms
  - [ ] Memory usage documented

- [ ] **Documentación**
  - [ ] ADR published
  - [ ] Storage strategy documented
  - [ ] Migration guide for future pgvector
  - [ ] Performance benchmarks

- [ ] **Validación Manual (OBLIGATORIA)**
  - [ ] Store embeddings for test video
  - [ ] Verify retrieval performance
  - [ ] Test similarity search accuracy
  - [ ] Compare results with Python baseline

### Dependencies

- **Bloqueantes:** YRUS-0602 (needs embeddings generated)
- **Bloquea a:** YRUS-0701 (search needs storage ready)

### Story Points Justification

- **Complexity:** Medium (decision-making, implementation straightforward)
- **Effort:** 4-5 hours development + 2-3 hours testing
- **Risk:** Low (using existing MySQL)
- **Decision Impact:** High (affects future scalability)

**Total: 5 pts**

---

## YRUS-0604: Batch Embedding Processing Job

**Story Points:** 5
**Priority:** High (P1)
**Sprint:** 3
**Epic:** Embedding Generation
**Git Branch:** `YRUS-0604_batch_embedding_job`

### User Story

**As a** system administrator
**I want** automatic batch embedding processing
**So that** all videos are embedded without manual intervention

### Acceptance Criteria

**AC1: Automatic Trigger**
- Given a video transcription completes
- When `Video.TranscriptionStatus = Completed`
- Then automatically enqueue `EmbeddingGenerationJob` if `AutoGenerateEmbeddings = true`
- And use same priority as transcription job
- And link Job.VideoId to video

**AC2: Batch Job for Multiple Videos**
- Given multiple videos awaiting embeddings
- When admin triggers batch job
- Then process all videos WHERE `TranscriptionStatus = Completed AND EmbeddingStatus = None`
- And process sequentially (avoid overload)
- And track overall batch progress
- And log summary at completion

**AC3: Re-Generation Support**
- Given videos with mock embeddings
- When re-generation is requested
- Then identify segments with mock embeddings
- And regenerate with real embeddings
- And preserve segment metadata
- And update EmbeddingStatus appropriately

**AC4: Progress Monitoring**
- Given batch embedding job
- When processing
- Then update Job progress per video
- And emit SignalR events per video
- And log batch statistics (videos/sec, segments/sec)
- And provide ETA for batch completion

**AC5: Error Recovery**
- Given embedding failures
- When errors occur
- Then retry failed videos (up to 2 attempts)
- And skip videos that consistently fail
- And log failed video IDs
- And continue with remaining videos
- And mark batch as "Partial Success" if >80% succeed

### Technical Notes

**File Structure:**
```
YoutubeRag.Application/
└── Jobs/
    └── BatchEmbeddingProcessor.cs (CREAR)

YoutubeRag.Infrastructure/
└── Jobs/
    └── BatchEmbeddingBackgroundJob.cs (CREAR)
```

**Job Orchestration:**
```
Transcription Job Completes
       ↓
Check AutoGenerateEmbeddings
       ↓
   [If True]
       ↓
Enqueue EmbeddingGenerationJob
       ↓
Process Video Embeddings
       ↓
Update Video.EmbeddingStatus
```

**Batch Processing:**
```csharp
public class BatchEmbeddingProcessor
{
    public async Task ProcessBatchAsync(CancellationToken ct)
    {
        // 1. Find videos needing embeddings
        var videos = await _videoRepository.GetVideosNeedingEmbeddingsAsync();

        _logger.LogInformation("Found {Count} videos needing embeddings", videos.Count);

        var successCount = 0;
        var failureCount = 0;

        // 2. Process each video
        foreach (var video in videos)
        {
            try
            {
                await _embeddingService.ProcessVideoEmbeddingsAsync(video.Id, ct);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process embeddings for video {VideoId}", video.Id);
                failureCount++;
            }
        }

        // 3. Log summary
        _logger.LogInformation(
            "Batch embedding completed: {Success} succeeded, {Failed} failed",
            successCount, failureCount);
    }
}
```

**Hangfire Recurring Job:**
```csharp
// Run nightly at 2 AM to catch any missed videos
RecurringJob.AddOrUpdate<BatchEmbeddingBackgroundJob>(
    "batch-embedding-nightly",
    job => job.ExecuteAsync(),
    Cron.Daily(2));
```

**Manual Trigger API:**
```csharp
[HttpPost("admin/embeddings/batch")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> TriggerBatchEmbedding()
{
    var jobId = BackgroundJob.Enqueue<BatchEmbeddingBackgroundJob>(j => j.ExecuteAsync());
    return Ok(new { JobId = jobId, Message = "Batch embedding job enqueued" });
}
```

### Definition of Done

- [ ] **Código Implementado**
  - [ ] `BatchEmbeddingProcessor` service
  - [ ] Hangfire background job
  - [ ] Automatic trigger after transcription
  - [ ] Manual trigger API endpoint
  - [ ] Recurring job configured
  - [ ] Code review completed

- [ ] **Testing**
  - [ ] Unit tests for batch logic
  - [ ] Integration test with multiple videos
  - [ ] Test automatic trigger
  - [ ] Test manual trigger
  - [ ] Error scenario testing
  - [ ] Test coverage >80%

- [ ] **Build y Deployment**
  - [ ] Build exitoso
  - [ ] Hangfire recurring job registered
  - [ ] Admin API accessible

- [ ] **Performance**
  - [ ] Batch processing: 1 video/minute (average)
  - [ ] No memory leaks in batch job
  - [ ] Proper resource cleanup

- [ ] **Documentación**
  - [ ] XML comments complete
  - [ ] Admin guide for batch operations
  - [ ] Troubleshooting guide

- [ ] **Validación Manual (OBLIGATORIA)**
  - [ ] Trigger automatic embedding after transcription
  - [ ] Trigger manual batch job
  - [ ] Verify all videos processed
  - [ ] Check error handling

### Dependencies

- **Bloqueantes:** YRUS-0602 (needs embedding generation logic)
- **Bloquea a:** None (independent feature)

### Story Points Justification

- **Complexity:** Medium (orchestration, error handling)
- **Effort:** 4-5 hours development + 2 hours testing
- **Risk:** Low (building on existing Hangfire infrastructure)

**Total: 5 pts**

---

## YRUS-0701: Vector Similarity Search

**Story Points:** 5
**Priority:** Critical (P0)
**Sprint:** 3
**Epic:** Search & Query
**Git Branch:** `YRUS-0701_similarity_search`

### User Story

**As a** user
**I want** to search video content semantically
**So that** I can find relevant segments even without exact keyword matches

### Acceptance Criteria

**AC1: Query Embedding Generation**
- Given a user search query (text)
- When search is requested
- Then generate query embedding using same ONNX model
- And normalize embedding (L2 norm)
- And validate embedding dimension (384)
- And cache query embeddings for repeated searches

**AC2: Similarity Calculation**
- Given query embedding and segment embeddings
- When calculating similarity
- Then use cosine similarity: `dot(A, B) / (norm(A) * norm(B))`
- And return similarity score in range [0, 1]
- And handle edge cases (zero vectors, NaN)
- And optimize calculation for performance

**AC3: Top-K Retrieval**
- Given similarity scores
- When retrieving results
- Then return top K most similar segments (default K=10)
- And sort by similarity descending
- And include segment metadata (text, timestamps, video info)
- And support pagination (offset, limit)

**AC4: Filtering**
- Given search query with filters
- When searching
- Then support filtering by VideoId (single video search)
- And support filtering by date range
- And support filtering by minimum similarity threshold (default: 0.5)
- And apply filters before similarity search (performance)

**AC5: Performance**
- Given similarity search request
- When executing
- Then complete in <1 second for single video (~200 segments)
- And complete in <3 seconds for all videos (<5000 segments)
- And support concurrent searches (10+ simultaneous)
- And log search performance metrics

### Technical Notes

**File Structure:**
```
YoutubeRag.Application/
├── Interfaces/Services/
│   └── ISimilaritySearchService.cs (CREAR)
├── Services/
│   └── InMemorySimilaritySearchService.cs (CREAR)
└── DTOs/Search/
    ├── SearchRequest.cs (CREAR)
    ├── SearchResult.cs (CREAR)
    └── SimilaritySearchResponse.cs (CREAR)
```

**Search Request DTO:**
```csharp
public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int TopK { get; set; } = 10;
    public string? VideoId { get; set; } // Optional filter
    public double MinSimilarity { get; set; } = 0.5;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Offset { get; set; } = 0;
    public int Limit { get; set; } = 100;
}
```

**Search Result DTO:**
```csharp
public class SearchResult
{
    public string SegmentId { get; set; }
    public string VideoId { get; set; }
    public string VideoTitle { get; set; }
    public string Text { get; set; }
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public double SimilarityScore { get; set; }
    public string? ThumbnailUrl { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class SimilaritySearchResponse
{
    public List<SearchResult> Results { get; set; } = new();
    public int TotalResults { get; set; }
    public double SearchDurationMs { get; set; }
    public string Query { get; set; } = string.Empty;
}
```

**Similarity Search Service:**
```csharp
public class InMemorySimilaritySearchService : ISimilaritySearchService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly ITranscriptSegmentRepository _segmentRepository;
    private readonly ILogger<InMemorySimilaritySearchService> _logger;
    private readonly IMemoryCache _cache;

    public async Task<SimilaritySearchResponse> SearchAsync(
        SearchRequest request,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        // 1. Generate query embedding
        var queryEmbedding = await GenerateQueryEmbeddingAsync(request.Query, ct);

        // 2. Load candidate segments (with filters)
        var segments = await LoadCandidateSegmentsAsync(request, ct);

        // 3. Calculate similarities
        var similarities = CalculateSimilarities(queryEmbedding, segments);

        // 4. Filter by minimum similarity
        var filtered = similarities
            .Where(s => s.Score >= request.MinSimilarity)
            .OrderByDescending(s => s.Score);

        // 5. Pagination
        var paginated = filtered
            .Skip(request.Offset)
            .Take(request.TopK)
            .ToList();

        stopwatch.Stop();

        return new SimilaritySearchResponse
        {
            Results = paginated,
            TotalResults = filtered.Count(),
            SearchDurationMs = stopwatch.Elapsed.TotalMilliseconds,
            Query = request.Query
        };
    }

    private async Task<float[]> GenerateQueryEmbeddingAsync(string query, CancellationToken ct)
    {
        // Cache query embeddings (common queries)
        var cacheKey = $"query_embedding:{query.GetHashCode()}";

        if (_cache.TryGetValue<float[]>(cacheKey, out var cached))
        {
            return cached;
        }

        var embedding = await _embeddingService.GenerateEmbeddingAsync(query, ct);

        _cache.Set(cacheKey, embedding, TimeSpan.FromMinutes(30));

        return embedding;
    }

    private async Task<List<SegmentWithEmbedding>> LoadCandidateSegmentsAsync(
        SearchRequest request,
        CancellationToken ct)
    {
        // Apply filters at database level
        var query = _segmentRepository.GetQueryable()
            .Where(s => s.EmbeddingVector != null);

        if (!string.IsNullOrEmpty(request.VideoId))
        {
            query = query.Where(s => s.VideoId == request.VideoId);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(s => s.CreatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(s => s.CreatedAt <= request.ToDate.Value);
        }

        var segments = await query
            .Include(s => s.Video)
            .Take(request.Limit)
            .ToListAsync(ct);

        // Deserialize embeddings
        return segments.Select(s => new SegmentWithEmbedding
        {
            Segment = s,
            Embedding = JsonSerializer.Deserialize<float[]>(s.EmbeddingVector!)!
        }).ToList();
    }

    private List<SearchResult> CalculateSimilarities(
        float[] queryEmbedding,
        List<SegmentWithEmbedding> segments)
    {
        return segments.Select(s => new SearchResult
        {
            SegmentId = s.Segment.Id,
            VideoId = s.Segment.VideoId,
            VideoTitle = s.Segment.Video.Title,
            Text = s.Segment.Text,
            StartTime = s.Segment.StartTime,
            EndTime = s.Segment.EndTime,
            SimilarityScore = CosineSimilarity(queryEmbedding, s.Embedding),
            ThumbnailUrl = s.Segment.Video.ThumbnailUrl
        }).ToList();
    }

    private double CosineSimilarity(float[] a, float[] b)
    {
        double dotProduct = 0.0;
        double normA = 0.0;
        double normB = 0.0;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA == 0.0 || normB == 0.0)
            return 0.0;

        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
```

**Performance Optimization:**
```csharp
// For large-scale search, consider:
// 1. Pre-filter candidates by metadata
// 2. Use approximate nearest neighbors (if pgvector later)
// 3. Cache frequent queries
// 4. Parallel similarity calculation
```

### Definition of Done

- [ ] **Código Implementado**
  - [ ] `ISimilaritySearchService` interface
  - [ ] `InMemorySimilaritySearchService` implementation
  - [ ] Search DTOs (Request, Result, Response)
  - [ ] Query embedding caching
  - [ ] Similarity calculation optimized
  - [ ] Code review completed

- [ ] **Testing**
  - [ ] Unit tests for similarity calculation
  - [ ] Unit tests for filtering logic
  - [ ] Integration test with real embeddings
  - [ ] Performance test with 1000+ segments
  - [ ] Test edge cases (zero vectors, empty results)
  - [ ] Test coverage >85%

- [ ] **Build y Deployment**
  - [ ] Build exitoso
  - [ ] Cache configured

- [ ] **Performance**
  - [ ] Single video search: <1 second
  - [ ] Multi-video search: <3 seconds
  - [ ] Concurrent searches supported (10+)
  - [ ] Memory usage reasonable

- [ ] **Documentación**
  - [ ] XML comments complete
  - [ ] Search API documented
  - [ ] Performance benchmarks
  - [ ] Example queries

- [ ] **Validación Manual (OBLIGATORIA)**
  - [ ] Search with various queries
  - [ ] Verify result relevance
  - [ ] Test filtering (by video, date)
  - [ ] Test pagination
  - [ ] Compare with expected results

### Dependencies

- **Bloqueantes:** YRUS-0601 (embedding service), YRUS-0602 (embeddings generated), YRUS-0603 (storage)
- **Bloquea a:** YRUS-0702 (API needs search service)

### Story Points Justification

- **Complexity:** Medium (similarity calculation, filtering)
- **Effort:** 5-6 hours development + 2-3 hours testing
- **Risk:** Low (well-understood algorithm)

**Total: 5 pts**

---

## YRUS-0702: Query API with Ranking

**Story Points:** 5
**Priority:** Critical (P0)
**Sprint:** 3
**Epic:** Search & Query
**Git Branch:** `YRUS-0702_query_api`

### User Story

**As a** API consumer
**I want** RESTful search endpoints with ranking
**So that** I can integrate semantic search into applications

### Acceptance Criteria

**AC1: Search Endpoint**
- Given API consumer
- When calling search endpoint
- Then expose `POST /api/v1/search/semantic`
- And accept `SearchRequest` JSON body
- And return `SimilaritySearchResponse` JSON
- And require JWT authentication
- And validate request parameters

**AC2: Request Validation**
- Given search request
- When validating
- Then require `Query` (min 3 chars, max 500 chars)
- And validate `TopK` (1-100, default 10)
- And validate `MinSimilarity` (0.0-1.0, default 0.5)
- And validate `VideoId` format (if provided)
- And return 400 Bad Request for invalid input

**AC3: Result Ranking**
- Given search results
- When returning
- Then sort by similarity score (descending)
- And apply secondary ranking: recency, view count, segment position
- And support ranking weights via configuration
- And include ranking metadata in response

**AC4: Response Format**
- Given search results
- When formatting response
- Then include segment text with highlighting (optional)
- And include video metadata (title, thumbnail, duration)
- And include timestamps for video navigation
- And include similarity score (0-1 range)
- And include total result count for pagination
- And include search duration for monitoring

**AC5: Error Handling**
- Given potential errors
- When search fails
- Then return 400 for invalid queries
- And return 404 if video not found (when filtering)
- And return 500 for system errors
- And log detailed error information
- And return user-friendly error messages

### Technical Notes

**File Structure:**
```
YoutubeRag.Api/
├── Controllers/
│   └── SearchController.cs (CREAR)
└── Validators/
    └── SearchRequestValidator.cs (CREAR)

YoutubeRag.Application/
└── Services/
    └── SearchRankingService.cs (CREAR)
```

**Search Controller:**
```csharp
[ApiController]
[Route("api/v1/search")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly ISimilaritySearchService _searchService;
    private readonly ISearchRankingService _rankingService;
    private readonly ILogger<SearchController> _logger;

    [HttpPost("semantic")]
    [ProducesResponseType(typeof(SimilaritySearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SimilaritySearchResponse>> SemanticSearch(
        [FromBody] SearchRequest request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Semantic search request: {Query}, TopK: {TopK}",
                request.Query, request.TopK);

            // 1. Similarity search
            var response = await _searchService.SearchAsync(request, ct);

            // 2. Apply advanced ranking
            response.Results = _rankingService.RankResults(response.Results, request);

            _logger.LogInformation("Search completed: {ResultCount} results in {Duration}ms",
                response.TotalResults, response.SearchDurationMs);

            return Ok(response);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Invalid search request");
            return BadRequest(new ErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed");
            return StatusCode(500, new ErrorResponse { Message = "Search failed" });
        }
    }

    [HttpGet("history")]
    [ProducesResponseType(typeof(List<SearchHistory>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SearchHistory>>> GetSearchHistory(CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var history = await _searchHistoryService.GetUserHistoryAsync(userId!, ct);
        return Ok(history);
    }
}
```

**Request Validation:**
```csharp
public class SearchRequestValidator : AbstractValidator<SearchRequest>
{
    public SearchRequestValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty().WithMessage("Query is required")
            .MinimumLength(3).WithMessage("Query must be at least 3 characters")
            .MaximumLength(500).WithMessage("Query must not exceed 500 characters");

        RuleFor(x => x.TopK)
            .InclusiveBetween(1, 100).WithMessage("TopK must be between 1 and 100");

        RuleFor(x => x.MinSimilarity)
            .InclusiveBetween(0.0, 1.0).WithMessage("MinSimilarity must be between 0.0 and 1.0");

        RuleFor(x => x.VideoId)
            .Must(BeValidGuid).When(x => !string.IsNullOrEmpty(x.VideoId))
            .WithMessage("VideoId must be a valid GUID");
    }

    private bool BeValidGuid(string? value)
    {
        return string.IsNullOrEmpty(value) || Guid.TryParse(value, out _);
    }
}
```

**Ranking Service:**
```csharp
public class SearchRankingService : ISearchRankingService
{
    public List<SearchResult> RankResults(List<SearchResult> results, SearchRequest request)
    {
        // Multi-factor ranking:
        // 1. Similarity score (primary, weight: 0.7)
        // 2. Video view count (secondary, weight: 0.2)
        // 3. Recency (tertiary, weight: 0.1)

        return results
            .Select(r => new
            {
                Result = r,
                RankScore = CalculateRankScore(r)
            })
            .OrderByDescending(x => x.RankScore)
            .Select(x => x.Result)
            .ToList();
    }

    private double CalculateRankScore(SearchResult result)
    {
        const double similarityWeight = 0.7;
        const double popularityWeight = 0.2;
        const double recencyWeight = 0.1;

        var similarityScore = result.SimilarityScore;
        var popularityScore = NormalizePopularity(result.Video.ViewCount);
        var recencyScore = NormalizeRecency(result.Video.PublishedAt);

        return (similarityScore * similarityWeight) +
               (popularityScore * popularityWeight) +
               (recencyScore * recencyWeight);
    }
}
```

**Swagger Documentation:**
```csharp
/// <summary>
/// Performs semantic search over video transcripts
/// </summary>
/// <param name="request">Search request with query and filters</param>
/// <returns>Ranked search results with similarity scores</returns>
/// <remarks>
/// Sample request:
///
///     POST /api/v1/search/semantic
///     {
///        "query": "machine learning explained",
///        "topK": 10,
///        "minSimilarity": 0.5,
///        "videoId": "optional-video-id-filter"
///     }
///
/// </remarks>
/// <response code="200">Returns search results</response>
/// <response code="400">Invalid request parameters</response>
/// <response code="500">Internal server error</response>
[HttpPost("semantic")]
```

### Definition of Done

- [ ] **Código Implementado**
  - [ ] `SearchController` complete
  - [ ] `SearchRequestValidator` implemented
  - [ ] `SearchRankingService` implemented
  - [ ] Error handling comprehensive
  - [ ] Swagger documentation complete
  - [ ] Code review completed

- [ ] **Testing**
  - [ ] Unit tests for controller
  - [ ] Unit tests for validation
  - [ ] Unit tests for ranking
  - [ ] Integration tests for API
  - [ ] Test error scenarios
  - [ ] Test coverage >85%

- [ ] **Build y Deployment**
  - [ ] Build exitoso
  - [ ] Swagger UI updated
  - [ ] API documented

- [ ] **Performance**
  - [ ] API response time: <1 second (P95)
  - [ ] Concurrent requests: 50+ RPS
  - [ ] Memory usage stable

- [ ] **Documentación**
  - [ ] XML comments on all endpoints
  - [ ] Swagger examples
  - [ ] Postman collection updated
  - [ ] Client integration guide

- [ ] **Validación Manual (OBLIGATORIA)**
  - [ ] Test API via Swagger
  - [ ] Test with Postman
  - [ ] Verify authentication
  - [ ] Test validation errors
  - [ ] Test ranking quality

### Dependencies

- **Bloqueantes:** YRUS-0701 (search service)
- **Bloquea a:** YRUS-0703 (context retrieval builds on search)

### Story Points Justification

- **Complexity:** Medium (API, validation, ranking)
- **Effort:** 5-6 hours development + 2-3 hours testing
- **Risk:** Low (standard REST API)

**Total: 5 pts**

---

## YRUS-0703: Context Retrieval for RAG

**Story Points:** 5
**Priority:** Critical (P0)
**Sprint:** 3
**Epic:** Search & Query
**Git Branch:** `YRUS-0703_context_retrieval`

### User Story

**As a** RAG system
**I want** to retrieve relevant context for LLM prompts
**So that** accurate answers can be generated from video content

### Acceptance Criteria

**AC1: Context Window Building**
- Given search results (top segments)
- When building context for LLM
- Then retrieve full segment text
- And include surrounding segments (±1 segment) for continuity
- And maintain chronological order within video
- And limit total context to 4000 tokens (configurable)
- And include segment metadata (timestamps, video title)

**AC2: Context Formatting**
- Given retrieved segments
- When formatting for LLM
- Then format as: `[Video: {title}] [{timestamp}] {text}`
- And separate segments with newlines
- And include source attribution
- And optionally include video metadata (author, date)
- And support multiple format templates (structured, conversational)

**AC3: Token Management**
- Given context segments
- When managing tokens
- Then estimate token count (1 token ≈ 4 chars)
- And prioritize highest-ranked segments
- And truncate if exceeding limit
- And preserve complete sentences
- And log token usage statistics

**AC4: Metadata Enrichment**
- Given context segments
- When enriching
- Then include video URLs for citation
- And include timestamps for video navigation
- And include confidence scores
- And include segment IDs for traceability
- And support custom metadata fields

**AC5: API Endpoint**
- Given context retrieval request
- When calling API
- Then expose `POST /api/v1/search/context`
- And accept query + parameters (max tokens, format)
- And return formatted context string
- And return metadata for citations
- And support streaming response (future)

### Technical Notes

**File Structure:**
```
YoutubeRag.Application/
├── Services/
│   └── ContextRetrievalService.cs (CREAR)
└── DTOs/Search/
    ├── ContextRequest.cs (CREAR)
    └── ContextResponse.cs (CREAR)

YoutubeRag.Api/
└── Controllers/
    └── SearchController.cs (ACTUALIZAR - add context endpoint)
```

**Context Request DTO:**
```csharp
public class ContextRequest
{
    public string Query { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 4000;
    public int TopK { get; set; } = 10;
    public string? VideoId { get; set; }
    public bool IncludeSurrounding { get; set; } = true;
    public ContextFormat Format { get; set; } = ContextFormat.Structured;
}

public enum ContextFormat
{
    Structured,      // Formal format with metadata
    Conversational,  // Natural language format
    JSON             // Structured JSON format
}
```

**Context Response DTO:**
```csharp
public class ContextResponse
{
    public string Context { get; set; } = string.Empty;
    public int TokenCount { get; set; }
    public List<ContextSource> Sources { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ContextSource
{
    public string SegmentId { get; set; }
    public string VideoId { get; set; }
    public string VideoTitle { get; set; }
    public string VideoUrl { get; set; }
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public double SimilarityScore { get; set; }
}
```

**Context Retrieval Service:**
```csharp
public class ContextRetrievalService : IContextRetrievalService
{
    private readonly ISimilaritySearchService _searchService;
    private readonly ITranscriptSegmentRepository _segmentRepository;
    private readonly ILogger<ContextRetrievalService> _logger;

    public async Task<ContextResponse> RetrieveContextAsync(
        ContextRequest request,
        CancellationToken ct)
    {
        // 1. Perform similarity search
        var searchRequest = new SearchRequest
        {
            Query = request.Query,
            TopK = request.TopK,
            VideoId = request.VideoId
        };

        var searchResults = await _searchService.SearchAsync(searchRequest, ct);

        // 2. Expand with surrounding segments (if enabled)
        var expandedSegments = request.IncludeSurrounding
            ? await ExpandWithSurroundingSegmentsAsync(searchResults.Results, ct)
            : searchResults.Results;

        // 3. Build context within token limit
        var context = BuildContext(expandedSegments, request);

        return context;
    }

    private async Task<List<SearchResult>> ExpandWithSurroundingSegmentsAsync(
        List<SearchResult> results,
        CancellationToken ct)
    {
        var expanded = new List<SearchResult>();

        foreach (var result in results)
        {
            // Get ±1 segment
            var surrounding = await _segmentRepository.GetSurroundingSegmentsAsync(
                result.SegmentId,
                before: 1,
                after: 1,
                ct);

            expanded.AddRange(surrounding);
        }

        // Deduplicate and sort by video + timestamp
        return expanded
            .DistinctBy(s => s.SegmentId)
            .OrderBy(s => s.VideoId)
            .ThenBy(s => s.StartTime)
            .ToList();
    }

    private ContextResponse BuildContext(
        List<SearchResult> segments,
        ContextRequest request)
    {
        var contextBuilder = new StringBuilder();
        var sources = new List<ContextSource>();
        int tokenCount = 0;

        foreach (var segment in segments)
        {
            var segmentText = FormatSegment(segment, request.Format);
            var segmentTokens = EstimateTokenCount(segmentText);

            if (tokenCount + segmentTokens > request.MaxTokens)
            {
                break; // Stop if exceeding token limit
            }

            contextBuilder.AppendLine(segmentText);
            contextBuilder.AppendLine();

            sources.Add(new ContextSource
            {
                SegmentId = segment.SegmentId,
                VideoId = segment.VideoId,
                VideoTitle = segment.VideoTitle,
                VideoUrl = $"https://youtube.com/watch?v={segment.VideoId}&t={(int)segment.StartTime}",
                StartTime = segment.StartTime,
                EndTime = segment.EndTime,
                SimilarityScore = segment.SimilarityScore
            });

            tokenCount += segmentTokens;
        }

        return new ContextResponse
        {
            Context = contextBuilder.ToString(),
            TokenCount = tokenCount,
            Sources = sources,
            Metadata = new Dictionary<string, object>
            {
                ["query"] = request.Query,
                ["segmentCount"] = sources.Count,
                ["format"] = request.Format.ToString()
            }
        };
    }

    private string FormatSegment(SearchResult segment, ContextFormat format)
    {
        return format switch
        {
            ContextFormat.Structured =>
                $"[Video: {segment.VideoTitle}] [{FormatTimestamp(segment.StartTime)}] {segment.Text}",

            ContextFormat.Conversational =>
                $"In the video \"{segment.VideoTitle}\" at {FormatTimestamp(segment.StartTime)}, " +
                $"the following was said: \"{segment.Text}\"",

            ContextFormat.JSON =>
                JsonSerializer.Serialize(new
                {
                    video = segment.VideoTitle,
                    timestamp = segment.StartTime,
                    text = segment.Text
                }),

            _ => segment.Text
        };
    }

    private int EstimateTokenCount(string text)
    {
        // Rough estimate: 1 token ≈ 4 characters
        return text.Length / 4;
    }

    private string FormatTimestamp(double seconds)
    {
        var timeSpan = TimeSpan.FromSeconds(seconds);
        return $"{(int)timeSpan.TotalMinutes}:{timeSpan.Seconds:D2}";
    }
}
```

**API Endpoint:**
```csharp
[HttpPost("context")]
[ProducesResponseType(typeof(ContextResponse), StatusCodes.Status200OK)]
public async Task<ActionResult<ContextResponse>> RetrieveContext(
    [FromBody] ContextRequest request,
    CancellationToken ct)
{
    var context = await _contextRetrievalService.RetrieveContextAsync(request, ct);
    return Ok(context);
}
```

**Example Response:**
```json
{
  "context": "[Video: Introduction to Machine Learning] [2:15] Machine learning is a subset of artificial intelligence...\n\n[Video: Introduction to Machine Learning] [5:30] There are three main types of machine learning: supervised, unsupervised, and reinforcement learning...\n",
  "tokenCount": 156,
  "sources": [
    {
      "segmentId": "seg-123",
      "videoId": "vid-456",
      "videoTitle": "Introduction to Machine Learning",
      "videoUrl": "https://youtube.com/watch?v=vid-456&t=135",
      "startTime": 135.0,
      "endTime": 150.0,
      "similarityScore": 0.92
    }
  ],
  "metadata": {
    "query": "what is machine learning",
    "segmentCount": 2,
    "format": "Structured"
  }
}
```

### Definition of Done

- [ ] **Código Implementado**
  - [ ] `ContextRetrievalService` complete
  - [ ] Context formatting logic
  - [ ] Surrounding segment retrieval
  - [ ] Token management
  - [ ] API endpoint
  - [ ] Code review completed

- [ ] **Testing**
  - [ ] Unit tests for context building
  - [ ] Unit tests for formatting
  - [ ] Unit tests for token estimation
  - [ ] Integration test with real segments
  - [ ] Test different formats
  - [ ] Test coverage >85%

- [ ] **Build y Deployment**
  - [ ] Build exitoso
  - [ ] API documented in Swagger

- [ ] **Performance**
  - [ ] Context retrieval: <500ms
  - [ ] Token estimation accurate (±10%)
  - [ ] Memory usage reasonable

- [ ] **Documentación**
  - [ ] XML comments complete
  - [ ] API usage examples
  - [ ] Format templates documented
  - [ ] LLM integration guide

- [ ] **Validación Manual (OBLIGATORIA)**
  - [ ] Retrieve context for sample query
  - [ ] Verify context quality
  - [ ] Test different formats
  - [ ] Verify token limits
  - [ ] Test citation URLs

### Dependencies

- **Bloqueantes:** YRUS-0701 (search service), YRUS-0702 (API)
- **Bloquea a:** None (final search feature)

### Story Points Justification

- **Complexity:** Medium (context building, formatting)
- **Effort:** 5-6 hours development + 2-3 hours testing
- **Risk:** Low (builds on existing search)

**Total: 5 pts**

---

## YRUS-0704: Search Result Optimization

**Story Points:** 2
**Priority:** Medium (P1)
**Sprint:** 3
**Epic:** Search & Query
**Git Branch:** `YRUS-0704_search_optimization`

### User Story

**As a** user
**I want** optimized and relevant search results
**So that** I quickly find the most useful content

### Acceptance Criteria

**AC1: Result Deduplication**
- Given search results with overlapping segments
- When returning results
- Then deduplicate segments from same timestamp range
- And prefer higher similarity scores
- And merge adjacent segments if beneficial

**AC2: Snippet Highlighting**
- Given search results
- When displaying
- Then highlight query terms in segment text (optional)
- And show text window around match
- And preserve sentence boundaries

**AC3: Result Caching**
- Given frequent queries
- When caching
- Then cache query results for 5 minutes
- And invalidate on new embeddings
- And cache by query hash
- And log cache hit rate

**AC4: Search Analytics**
- Given search requests
- When tracking
- Then log query, result count, duration
- And track popular queries
- And track zero-result queries
- And provide analytics endpoint for admins

### Technical Notes

Simple optimizations to improve search experience. Low priority but high impact on UX.

### Definition of Done

- [ ] **Código Implementado**
  - [ ] Deduplication logic
  - [ ] Caching implemented
  - [ ] Analytics logging
  - [ ] Code review completed

- [ ] **Testing**
  - [ ] Unit tests
  - [ ] Performance tests
  - [ ] Test coverage >70%

- [ ] **Validación Manual**
  - [ ] Test deduplication
  - [ ] Verify caching works
  - [ ] Check analytics logs

### Dependencies

- **Bloqueantes:** YRUS-0702 (API)
- **Bloquea a:** None

### Story Points Justification

- **Complexity:** Low (incremental improvements)
- **Effort:** 2-3 hours development + 1 hour testing

**Total: 2 pts**

---

## YRUS-0801: End-to-End Search Testing

**Story Points:** 5
**Priority:** Critical (P0)
**Sprint:** 3
**Epic:** Integration & Quality
**Git Branch:** `YRUS-0801_e2e_search_testing`

### User Story

**As a** QA engineer
**I want** comprehensive end-to-end search testing
**So that** we ensure search quality and reliability

### Acceptance Criteria

**AC1: Integration Test Suite**
- Given complete search pipeline
- When running integration tests
- Then test embedding generation end-to-end
- And test similarity search with real data
- And test API endpoints
- And test error scenarios

**AC2: Test Data Preparation**
- Given need for test data
- When preparing
- Then create test videos with known content
- And generate embeddings
- And define expected search results
- And include edge cases

**AC3: Quality Metrics**
- Given search tests
- When measuring quality
- Then calculate precision@K
- And calculate recall@K
- And measure latency (P50, P95, P99)
- And track search quality score

**AC4: Regression Testing**
- Given search implementation
- When running regression
- Then verify no quality degradation
- And verify performance stable
- And compare with baseline

### Technical Notes

Comprehensive testing to ensure production readiness. Critical for MVP launch.

### Definition of Done

- [ ] **Testing**
  - [ ] Integration test suite complete
  - [ ] Test data prepared
  - [ ] Quality metrics calculated
  - [ ] Regression baseline established
  - [ ] All tests passing

- [ ] **Documentación**
  - [ ] Test documentation
  - [ ] Quality metrics report
  - [ ] Regression test guide

- [ ] **Validación Manual**
  - [ ] Manual search testing
  - [ ] Quality assessment
  - [ ] Performance validation

### Dependencies

- **Bloqueantes:** YRUS-0701, YRUS-0702, YRUS-0703
- **Bloquea a:** None (final testing)

### Story Points Justification

- **Complexity:** Medium (comprehensive testing)
- **Effort:** 5-6 hours development + testing

**Total: 5 pts**

---

## YRUS-0802: Performance Benchmarking

**Story Points:** 3
**Priority:** High (P1)
**Sprint:** 3
**Epic:** Integration & Quality
**Git Branch:** `YRUS-0802_performance_benchmarking`

### User Story

**As a** system administrator
**I want** performance benchmarks documented
**So that** we can monitor and optimize the system

### Acceptance Criteria

**AC1: Benchmark Suite**
- Given performance requirements
- When benchmarking
- Then measure embedding generation throughput
- And measure search latency
- And measure concurrent request handling
- And measure memory usage

**AC2: Load Testing**
- Given system under load
- When testing
- Then simulate 50+ concurrent searches
- And test with 10+ videos (5000+ segments)
- And measure degradation under load

**AC3: Baseline Documentation**
- Given benchmark results
- When documenting
- Then record all metrics
- And compare with targets
- And identify bottlenecks
- And recommend optimizations

### Technical Notes

Performance validation and baseline establishment. Important for production planning.

### Definition of Done

- [ ] **Testing**
  - [ ] Benchmark suite implemented
  - [ ] Load tests executed
  - [ ] Results documented

- [ ] **Documentación**
  - [ ] Performance baseline report
  - [ ] Bottleneck analysis
  - [ ] Optimization recommendations

### Dependencies

- **Bloqueantes:** All Epic 6, 7 stories
- **Bloquea a:** None

### Story Points Justification

- **Complexity:** Low (measurement and documentation)
- **Effort:** 3-4 hours testing + reporting

**Total: 3 pts**

---

## Sprint 3 Roadmap

### Day-by-Day Plan

**Day 1 (Oct 10 - Thursday):**
- **Morning:** YRUS-0601 (Configure ONNX Model) - 5 pts
- **Afternoon:** Start YRUS-0602 (Generate Embeddings) - partial

**Day 2 (Oct 13 - Monday):**
- **Morning:** Complete YRUS-0602 - 8 pts
- **Afternoon:** YRUS-0603 (Vector Storage) - 5 pts

**Day 3 (Oct 14 - Tuesday):**
- **Morning:** YRUS-0604 (Batch Embedding Job) - 5 pts
- **Afternoon:** Start YRUS-0701 (Similarity Search) - partial

**Day 4 (Oct 15 - Wednesday):**
- **Morning:** Complete YRUS-0701 - 5 pts
- **Afternoon:** YRUS-0702 (Query API) - 5 pts

**Day 5 (Oct 16 - Thursday):**
- **Morning:** YRUS-0703 (Context Retrieval) - 5 pts
- **Afternoon:** YRUS-0704 (Optimization) - 2 pts
- **Evening:** Start testing

**Day 6 (Oct 17 - Friday):**
- **Morning:** YRUS-0801 (E2E Testing) - 5 pts
- **Afternoon:** YRUS-0802 (Benchmarking) - 3 pts
- **Evening:** Sprint review & retrospective

---

## Dependencies Graph

```
YRUS-0601 (ONNX Model)
    ↓
YRUS-0602 (Generate Embeddings)
    ↓
YRUS-0603 (Vector Storage)
    ↓
    ├→ YRUS-0604 (Batch Job)
    └→ YRUS-0701 (Similarity Search)
              ↓
         YRUS-0702 (Query API)
              ↓
         YRUS-0703 (Context Retrieval)
              ↓
         YRUS-0704 (Optimization)
              ↓
         ┌────┴─────┐
    YRUS-0801   YRUS-0802
    (Testing)   (Benchmarking)
```

---

## Technical Recommendations

### Embedding Model Selection

**Recommended: all-MiniLM-L6-v2**

**Pros:**
- Small size (~90 MB)
- Fast inference (1000+ embeddings/sec on CPU)
- Good quality for general-purpose search
- Well-supported in ONNX format
- Low memory footprint

**Alternatives:**
- **all-mpnet-base-v2:** Better quality, slower (768 dimensions)
- **multilingual-e5-small:** Multi-language support
- **OpenAI text-embedding-3-small:** Best quality, API costs

### Vector Storage Recommendation

**MVP: MySQL JSON + In-Memory Search**

**Rationale:**
1. **Zero infrastructure changes:** Works with existing MySQL
2. **Fast implementation:** 1-2 days vs 5+ days for migration
3. **Sufficient for MVP scale:** <5000 segments searches in <1 second
4. **Easy migration path:** Can upgrade to pgvector later

**Future Migration (Sprint 4+):**
- Consider pgvector if >10 videos or >10k segments
- Consider Qdrant/Milvus if >100 videos

### ONNX vs Python Integration

**Recommended: ONNX Runtime (C#)**

**Pros:**
- Native C# integration
- No Python dependency
- Better performance
- Easier deployment

**Fallback: Python via pythonnet**
- Use if ONNX proves complex
- Requires Python runtime
- More dependencies
- Acceptable for MVP

---

## Risk Analysis

### Risk 1: ONNX Integration Complexity

**Probability:** Medium
**Impact:** High

**Mitigation:**
- Allocate 1 day for ONNX setup
- Have Python fallback ready
- Use well-documented models
- Test early in sprint

### Risk 2: Search Performance Insufficient

**Probability:** Low
**Impact:** Medium

**Mitigation:**
- Start with in-memory search (simple)
- Benchmark early
- Have pgvector migration plan ready
- Optimize only if needed

### Risk 3: Embedding Quality Not Meeting Expectations

**Probability:** Low
**Impact:** High

**Mitigation:**
- Use proven model (all-MiniLM-L6-v2)
- Compare with Python baseline
- Manual quality testing
- User feedback loop

### Risk 4: Token Management Complexity

**Probability:** Low
**Impact:** Low

**Mitigation:**
- Simple estimation algorithm (chars/4)
- Validate with real LLM later
- Conservative limits (4000 tokens)

---

## Success Criteria

### Functional Requirements

- [ ] **Embedding Generation:** All segments have real embeddings (384-dim)
- [ ] **Search Works:** Semantic search returns relevant results
- [ ] **API Functional:** Query API exposed and documented
- [ ] **Context Retrieval:** Context formatted for LLM integration
- [ ] **Performance:** Search completes in <1 second (single video)

### Quality Requirements

- [ ] **Test Coverage:** 70%+ overall, 85%+ for search services
- [ ] **Build Quality:** 0 errors, 0 warnings
- [ ] **API Documentation:** Swagger complete with examples
- [ ] **Integration Tests:** E2E search test passing
- [ ] **Manual Testing:** 10+ search queries validated

### Performance Requirements

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Embedding Generation** | >1000/sec on CPU | Benchmark test |
| **Single Video Search** | <1 second | Integration test |
| **Multi-Video Search** | <3 seconds | Load test |
| **Concurrent Searches** | 50+ RPS | Load test |
| **Memory Usage** | <2 GB | Monitoring |

### Business Requirements

- [ ] **MVP Complete:** All core RAG features functional
- [ ] **User Value:** Users can search video content semantically
- [ ] **Technical Debt:** <5 critical issues
- [ ] **Documentation:** Complete API docs + guides
- [ ] **Production Ready:** Deployable to staging

---

## Definition of Done (Sprint-Level)

### Code Quality

- [ ] All user stories completed (48/48 pts)
- [ ] Code reviewed and approved
- [ ] 0 compiler errors, 0 warnings
- [ ] Clean Architecture maintained
- [ ] SOLID principles followed

### Testing

- [ ] Unit test coverage: 70%+
- [ ] Integration tests: All critical paths
- [ ] E2E test: Complete search flow
- [ ] Performance tests: All targets met
- [ ] Manual testing: Completed and documented

### Documentation

- [ ] XML comments on all public APIs
- [ ] Swagger documentation complete
- [ ] API usage guide updated
- [ ] Architecture decisions recorded (ADRs)
- [ ] Performance benchmarks documented

### Deployment

- [ ] Build successful (Release configuration)
- [ ] Deployed to staging environment
- [ ] Database migrations executed
- [ ] Configuration documented
- [ ] Smoke tests passed

### Sprint Artifacts

- [ ] Sprint review conducted
- [ ] Demo to stakeholders
- [ ] Sprint retrospective completed
- [ ] Velocity calculated
- [ ] Sprint 4 planned (if needed)

---

## Post-Sprint Activities

### Sprint Review (Oct 17, 3:00 PM)

**Agenda:**
1. Demo: Semantic search end-to-end (15 min)
2. Metrics review: Story points, velocity, quality (10 min)
3. Technical achievements: ONNX integration, search quality (10 min)
4. Stakeholder feedback (10 min)
5. Next steps discussion (5 min)

**Deliverables:**
- Working semantic search demo
- Performance benchmarks report
- Sprint completion report
- Known issues list

### Sprint Retrospective (Oct 17, 4:00 PM)

**Topics:**
- What went well?
- What could be improved?
- ONNX integration learnings
- Search quality assessment
- Agent-driven development effectiveness
- Sprint 4 planning

---

## Sprint 4 Planning (Optional)

**If Sprint 3 completes early or scope expands:**

### Potential Sprint 4 Focus Areas

**Option A: Quality & Polish (5 days)**
- Increase test coverage to 80%+
- Performance optimization
- Bug fixes and refinements
- Production deployment preparation

**Option B: Advanced Features (5 days)**
- Multi-language support
- Video summarization
- Batch video processing
- Analytics dashboard

**Option C: Production Readiness (5 days)**
- Monitoring and alerting
- Logging enhancements
- Security hardening
- Load testing at scale

**Decision Point:** End of Sprint 3 review

---

## Conclusion

Sprint 3 represents the **final core feature delivery** for the YouTube RAG MVP. With successful completion of embedding generation and semantic search, the system will be **feature-complete** and ready for production deployment.

### Expected Outcomes

- **Real Embeddings:** All transcript segments have semantic vectors
- **Semantic Search:** Users can find content by meaning, not just keywords
- **RAG Foundation:** Context retrieval ready for LLM integration
- **Production Ready:** Tested, documented, and deployable

### Success Metrics

- **Story Points:** 48 pts committed
- **Velocity:** 8-10 pts/day (conservative estimate)
- **Quality:** A grade (>90%)
- **Test Coverage:** 70%+
- **Performance:** All targets met

### Next Milestone

**Post-Sprint 3:** MVP Launch or Sprint 4 (Polish & Production Readiness)

---

**Document Status:** READY FOR SPRINT KICKOFF
**Next Review:** Daily standups + end of sprint
**Prepared By:** Senior Product Owner
**Approved By:** Technical Lead + Stakeholders

---

**End of Sprint 3 Plan**
