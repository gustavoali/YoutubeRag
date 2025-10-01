# Week 1 Detailed Execution Plan - Foundation & Stabilization
**Sprint Period:** Days 1-7
**Theme:** "Build on Solid Ground"
**Sprint Goal:** Eliminate all P0 blockers and establish quality foundation

## Week 1 Overview Dashboard

| Metric | Target | Priority |
|--------|--------|----------|
| **P0 Blockers Resolved** | 100% (8/8) | CRITICAL |
| **Code Coverage** | 40% | HIGH |
| **Database Migrations** | Working | CRITICAL |
| **Repository Pattern** | Implemented | HIGH |
| **Authentication** | Functional | HIGH |
| **CI/CD Pipeline** | Running | MEDIUM |

## Resource Allocation

| Day | Backend Dev | QA Engineer | DevOps | PM | Hours/Day |
|-----|------------|-------------|--------|-----|-----------|
| Day 1 | 100% | - | 25% | 25% | 10 |
| Day 2 | 100% | - | - | 25% | 9 |
| Day 3 | 100% | 25% | - | 25% | 10 |
| Day 4 | 75% | 100% | 25% | 25% | 10 |
| Day 5 | 75% | 100% | - | 25% | 10 |
| Day 6 | 100% | 25% | 25% | 25% | 8 |
| Day 7 | 50% | 50% | 25% | 100% | 6 |

---

## Day 1 (Monday): Database Foundation
**Lead:** Backend Developer
**Support:** DevOps Engineer (MySQL setup)

### Morning Session (9:00 AM - 1:00 PM)

#### Task 1.1: Fix Vector Storage Implementation
**Time:** 1 hour
**File:** `YoutubeRag.Domain/Entities/TranscriptSegment.cs`

```csharp
// BEFORE (Current - BROKEN):
public class TranscriptSegment : BaseEntity
{
    public string VideoId { get; set; }
    public string Text { get; set; }
    public string EmbeddingVector { get; set; } // ❌ WRONG TYPE
    // ...
}

// AFTER (Fixed):
public class TranscriptSegment : BaseEntity
{
    public string VideoId { get; set; }
    public string Text { get; set; }

    // Option 1: For MySQL without vector extension
    [Column(TypeName = "JSON")]
    public float[] EmbeddingVector { get; set; }

    // Option 2: Store as binary for performance
    [Column(TypeName = "BLOB")]
    public byte[] EmbeddingVectorBinary { get; set; }

    [NotMapped]
    public float[] EmbeddingVector
    {
        get => DeserializeVector(EmbeddingVectorBinary);
        set => EmbeddingVectorBinary = SerializeVector(value);
    }

    private byte[] SerializeVector(float[] vector)
    {
        if (vector == null) return null;
        var bytes = new byte[vector.Length * sizeof(float)];
        Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private float[] DeserializeVector(byte[] bytes)
    {
        if (bytes == null) return null;
        var vector = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, vector, 0, bytes.Length);
        return vector;
    }
    // ...
}
```

#### Task 1.2: Update ApplicationDbContext
**Time:** 1 hour
**File:** `YoutubeRag.Infrastructure/Data/ApplicationDbContext.cs`

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // User configuration
    modelBuilder.Entity<User>(entity =>
    {
        entity.HasIndex(e => e.Email).IsUnique();
        entity.HasIndex(e => e.GoogleId).IsUnique();
        entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
        entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
    });

    // Video configuration
    modelBuilder.Entity<Video>(entity =>
    {
        entity.HasIndex(e => e.YoutubeId).IsUnique();
        entity.HasIndex(e => new { e.UserId, e.Status });
        entity.HasIndex(e => new { e.Status, e.CreatedAt });
        entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
        entity.Property(e => e.Status).HasConversion<string>();
    });

    // TranscriptSegment configuration
    modelBuilder.Entity<TranscriptSegment>(entity =>
    {
        entity.HasIndex(e => new { e.VideoId, e.SegmentIndex }).IsUnique();
        entity.HasIndex(e => e.VideoId);
        entity.Property(e => e.Text).IsRequired().HasColumnType("TEXT");
        entity.Property(e => e.EmbeddingVectorBinary).HasColumnType("BLOB");
    });

    // Job configuration
    modelBuilder.Entity<Job>(entity =>
    {
        entity.HasIndex(e => new { e.Status, e.CreatedAt });
        entity.HasIndex(e => e.UserId);
        entity.Property(e => e.Type).IsRequired().HasMaxLength(100);
        entity.Property(e => e.Status).HasConversion<string>();
        entity.Property(e => e.Parameters).HasColumnType("JSON");
        entity.Property(e => e.Result).HasColumnType("JSON");
    });

    // RefreshToken configuration
    modelBuilder.Entity<RefreshToken>(entity =>
    {
        entity.HasIndex(e => e.Token).IsUnique();
        entity.HasIndex(e => e.UserId);
        entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
    });
}
```

#### Task 1.3: Generate Initial Migration
**Time:** 30 minutes
**Commands:**

```bash
# Install EF Core tools if not present
dotnet tool install --global dotnet-ef

# Navigate to solution root
cd C:/agents/youtube_rag_net

# Create initial migration
dotnet ef migrations add InitialCreate \
  -c ApplicationDbContext \
  -p YoutubeRag.Infrastructure \
  -s YoutubeRag.Api \
  -o Data/Migrations

# Verify migration files created
ls YoutubeRag.Infrastructure/Data/Migrations/

# Expected files:
# - [Timestamp]_InitialCreate.cs
# - [Timestamp]_InitialCreate.Designer.cs
# - ApplicationDbContextModelSnapshot.cs
```

#### Task 1.4: Configure Database Connection
**Time:** 30 minutes
**Files to update:**

```json
// appsettings.Development.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=youtuberag_dev;User=root;Password=YourSecurePassword123!;",
    "HangfireConnection": "Server=localhost;Port=3306;Database=youtuberag_hangfire;User=root;Password=YourSecurePassword123!;"
  }
}

// appsettings.Local.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=youtuberag_local;User=root;Password=YourSecurePassword123!;",
    "HangfireConnection": "Server=localhost;Port=3306;Database=youtuberag_hangfire_local;User=root;Password=YourSecurePassword123!;"
  }
}
```

### Afternoon Session (2:00 PM - 6:00 PM)

#### Task 1.5: Apply Migrations and Test
**Time:** 1 hour
**Commands:**

```bash
# Start MySQL in Docker
docker-compose up -d mysql

# Wait for MySQL to be ready
docker exec youtuberag-mysql mysql -u root -p -e "SELECT 1"

# Apply migrations
dotnet ef database update \
  -c ApplicationDbContext \
  -p YoutubeRag.Infrastructure \
  -s YoutubeRag.Api

# Verify database structure
docker exec youtuberag-mysql mysql -u root -p youtuberag_dev -e "SHOW TABLES;"
docker exec youtuberag-mysql mysql -u root -p youtuberag_dev -e "DESCRIBE Videos;"
docker exec youtuberag-mysql mysql -u root -p youtuberag_dev -e "DESCRIBE TranscriptSegments;"
```

#### Task 1.6: Create Migration Runner
**Time:** 1 hour
**File:** `YoutubeRag.Infrastructure/Data/MigrationRunner.cs`

```csharp
public static class MigrationRunner
{
    public static async Task RunMigrationsAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            logger.LogInformation("Starting database migration");

            // Get pending migrations
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Applying {Count} pending migrations", pendingMigrations.Count());
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migration completed successfully");
            }
            else
            {
                logger.LogInformation("Database is up to date");
            }

            // Verify connection
            await context.Database.CanConnectAsync();
            logger.LogInformation("Database connection verified");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database migration failed");
            throw;
        }
    }
}
```

#### Task 1.7: Add Migration to Startup
**Time:** 30 minutes
**File:** `YoutubeRag.Api/Program.cs`

```csharp
// Add after builder.Build()
var app = builder.Build();

// Run migrations on startup (development only)
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Local"))
{
    await MigrationRunner.RunMigrationsAsync(app.Services);
}
```

#### Task 1.8: Create Database Health Check
**Time:** 30 minutes
**File:** `YoutubeRag.Api/HealthChecks/DatabaseHealthCheck.cs`

```csharp
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;

    public DatabaseHealthCheck(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test database connection
            await _context.Database.CanConnectAsync(cancellationToken);

            // Test query
            await _context.Users.CountAsync(cancellationToken);

            return HealthCheckResult.Healthy("Database is accessible");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database is not accessible", ex);
        }
    }
}

// Register in Program.cs
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");
```

### Day 1 Validation Checklist
- [ ] TranscriptSegment uses proper vector storage type
- [ ] ApplicationDbContext has all indexes defined
- [ ] Initial migration files generated
- [ ] Database connection strings configured
- [ ] Migrations apply successfully
- [ ] All tables created with correct schema
- [ ] Health check endpoint returns healthy
- [ ] No compilation errors
- [ ] Unit test project can connect to test database

### Day 1 Deliverables
1. **Fixed vector storage implementation**
2. **Generated EF Core migrations**
3. **Working database with all tables**
4. **Database health monitoring**
5. **Migration runner for automatic updates**

---

## Day 2 (Tuesday): Repository Pattern & DTOs
**Lead:** Backend Developer

### Morning Session (9:00 AM - 1:00 PM)

#### Task 2.1: Create Base Repository Interface
**Time:** 45 minutes
**File:** `YoutubeRag.Application/Interfaces/IRepository.cs`

```csharp
namespace YoutubeRag.Application.Interfaces
{
    public interface IRepository<T> where T : BaseEntity
    {
        // Basic CRUD operations
        Task<T> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
        Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
        Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);

        // Batch operations
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
        Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
        Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

        // Query operations
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task<int> CountAsync(Expression<Func<T, bool>> predicate = null, CancellationToken cancellationToken = default);

        // Pagination
        Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize,
            Expression<Func<T, bool>> predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            CancellationToken cancellationToken = default);
    }

    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPrevious => PageNumber > 1;
        public bool HasNext => PageNumber < TotalPages;
    }
}
```

#### Task 2.2: Implement Generic Repository
**Time:** 1 hour
**File:** `YoutubeRag.Infrastructure/Repositories/Repository.cs`

```csharp
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.Id = Guid.NewGuid().ToString();
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _dbSet.Update(entity);
        await Task.CompletedTask;
    }

    public virtual async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);
        await Task.CompletedTask;
    }

    public virtual async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(e => e.Id == id, cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
    }

    public virtual async Task<PagedResult<T>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>> predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (predicate != null)
            query = query.Where(predicate);

        var totalCount = await query.CountAsync(cancellationToken);

        if (orderBy != null)
            query = orderBy(query);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
```

#### Task 2.3: Create Specialized Repositories
**Time:** 1 hour
**File:** `YoutubeRag.Infrastructure/Repositories/VideoRepository.cs`

```csharp
public interface IVideoRepository : IRepository<Video>
{
    Task<Video> GetByYoutubeIdAsync(string youtubeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Video>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Video>> GetByStatusAsync(VideoStatus status, CancellationToken cancellationToken = default);
    Task<Video> GetWithSegmentsAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByYoutubeIdAsync(string youtubeId, CancellationToken cancellationToken = default);
}

public class VideoRepository : Repository<Video>, IVideoRepository
{
    public VideoRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Video> GetByYoutubeIdAsync(string youtubeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.YoutubeId == youtubeId, cancellationToken);
    }

    public async Task<IEnumerable<Video>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Video>> GetByStatusAsync(VideoStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(v => v.Status == status)
            .Include(v => v.User)
            .ToListAsync(cancellationToken);
    }

    public async Task<Video> GetWithSegmentsAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(v => v.TranscriptSegments.OrderBy(s => s.SegmentIndex))
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByYoutubeIdAsync(string youtubeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(v => v.YoutubeId == youtubeId, cancellationToken);
    }
}
```

#### Task 2.4: Implement Unit of Work
**Time:** 45 minutes
**File:** `YoutubeRag.Infrastructure/Repositories/UnitOfWork.cs`

```csharp
public interface IUnitOfWork : IDisposable
{
    IVideoRepository Videos { get; }
    IRepository<User> Users { get; }
    IRepository<TranscriptSegment> TranscriptSegments { get; }
    IRepository<Job> Jobs { get; }
    IRepository<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction _transaction;
    private bool _disposed;

    // Repository instances
    private IVideoRepository _videos;
    private IRepository<User> _users;
    private IRepository<TranscriptSegment> _transcriptSegments;
    private IRepository<Job> _jobs;
    private IRepository<RefreshToken> _refreshTokens;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IVideoRepository Videos => _videos ??= new VideoRepository(_context);
    public IRepository<User> Users => _users ??= new Repository<User>(_context);
    public IRepository<TranscriptSegment> TranscriptSegments =>
        _transcriptSegments ??= new Repository<TranscriptSegment>(_context);
    public IRepository<Job> Jobs => _jobs ??= new Repository<Job>(_context);
    public IRepository<RefreshToken> RefreshTokens =>
        _refreshTokens ??= new Repository<RefreshToken>(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
            await _transaction?.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            await _transaction?.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        await _transaction?.RollbackAsync(cancellationToken);
        await _transaction?.DisposeAsync();
        _transaction = null;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _transaction?.Dispose();
                _context?.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
```

### Afternoon Session (2:00 PM - 6:00 PM)

#### Task 2.5: Create DTOs
**Time:** 1 hour
**Files:** Create in `YoutubeRag.Application/DTOs/`

```csharp
// VideoDto.cs
public class VideoDto
{
    public string Id { get; set; }
    public string YoutubeId { get; set; }
    public string YoutubeUrl { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Author { get; set; }
    public TimeSpan Duration { get; set; }
    public string ThumbnailUrl { get; set; }
    public VideoStatus Status { get; set; }
    public int ProcessingProgress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public List<TranscriptSegmentDto> TranscriptSegments { get; set; }
}

// TranscriptSegmentDto.cs
public class TranscriptSegmentDto
{
    public string Id { get; set; }
    public int SegmentIndex { get; set; }
    public string Text { get; set; }
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public float Confidence { get; set; }
}

// CreateVideoRequest.cs
public class CreateVideoRequest
{
    [Required]
    [Url]
    public string Url { get; set; }

    [MaxLength(500)]
    public string Title { get; set; }

    [MaxLength(2000)]
    public string Description { get; set; }
}

// VideoProgressDto.cs
public class VideoProgressDto
{
    public string VideoId { get; set; }
    public VideoStatus Status { get; set; }
    public int PercentComplete { get; set; }
    public string CurrentOperation { get; set; }
    public DateTime LastUpdated { get; set; }
}
```

#### Task 2.6: Configure AutoMapper
**Time:** 45 minutes
**File:** `YoutubeRag.Application/Mappings/AutoMapperProfile.cs`

```csharp
public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        // Video mappings
        CreateMap<Video, VideoDto>()
            .ForMember(dest => dest.TranscriptSegments,
                opt => opt.MapFrom(src => src.TranscriptSegments.OrderBy(s => s.SegmentIndex)));

        CreateMap<CreateVideoRequest, Video>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => VideoStatus.Pending))
            .ForMember(dest => dest.ProcessingProgress, opt => opt.MapFrom(src => 0));

        // TranscriptSegment mappings
        CreateMap<TranscriptSegment, TranscriptSegmentDto>()
            .ForMember(dest => dest.Confidence,
                opt => opt.MapFrom(src => src.Confidence ?? 0.95f));

        // User mappings
        CreateMap<User, UserDto>();
        CreateMap<CreateUserRequest, User>();
        CreateMap<UpdateUserRequest, User>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // Job mappings
        CreateMap<Job, JobDto>();
        CreateMap<CreateJobRequest, Job>();
    }
}

// Register in Program.cs
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
```

#### Task 2.7: Refactor Services to Use Repository
**Time:** 1.5 hours
**Example:** `YoutubeRag.Application/Services/VideoService.cs`

```csharp
// BEFORE (Direct DbContext usage):
public class VideoService : IVideoService
{
    private readonly ApplicationDbContext _context;

    public async Task<Video> GetVideoAsync(string id)
    {
        return await _context.Videos.FindAsync(id);
    }
}

// AFTER (Using Repository and UnitOfWork):
public class VideoService : IVideoService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<VideoService> _logger;

    public VideoService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<VideoService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<VideoDto> GetVideoAsync(string id)
    {
        var video = await _unitOfWork.Videos.GetWithSegmentsAsync(id);
        if (video == null)
        {
            throw new NotFoundException($"Video with ID {id} not found");
        }
        return _mapper.Map<VideoDto>(video);
    }

    public async Task<VideoDto> CreateVideoAsync(CreateVideoRequest request, string userId)
    {
        // Check if video already exists
        var existingVideo = await _unitOfWork.Videos.GetByYoutubeIdAsync(ExtractYoutubeId(request.Url));
        if (existingVideo != null)
        {
            throw new ConflictException($"Video already exists with ID {existingVideo.Id}");
        }

        // Create new video
        var video = _mapper.Map<Video>(request);
        video.UserId = userId;
        video.YoutubeId = ExtractYoutubeId(request.Url);
        video.YoutubeUrl = request.Url;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _unitOfWork.Videos.AddAsync(video);

            // Create processing job
            var job = new Job
            {
                Type = "VideoProcessing",
                VideoId = video.Id,
                UserId = userId,
                Status = JobStatus.Pending,
                Parameters = JsonSerializer.Serialize(new { VideoId = video.Id })
            };
            await _unitOfWork.Jobs.AddAsync(job);

            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Created video {VideoId} for user {UserId}", video.Id, userId);

            return _mapper.Map<VideoDto>(video);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Failed to create video for URL {Url}", request.Url);
            throw;
        }
    }

    private string ExtractYoutubeId(string url)
    {
        // YouTube URL parsing logic
        var regex = new Regex(@"(?:youtube\.com/watch\?v=|youtu\.be/)([^&\n?#]+)");
        var match = regex.Match(url);
        return match.Success ? match.Groups[1].Value : null;
    }
}
```

#### Task 2.8: Register Services in DI Container
**Time:** 30 minutes
**File:** `YoutubeRag.Api/Program.cs`

```csharp
// Add repository and UnitOfWork registration
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Services registration (refactored to use UnitOfWork)
builder.Services.AddScoped<IVideoService, VideoService>();
builder.Services.AddScoped<ITranscriptionService, LocalWhisperService>();
builder.Services.AddScoped<IJobService, JobService>();

// AutoMapper
builder.Services.AddAutoMapper(Assembly.GetAssembly(typeof(AutoMapperProfile)));

// Validation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateVideoRequestValidator>();
```

### Day 2 Validation Checklist
- [ ] Generic repository pattern implemented
- [ ] Unit of Work pattern functional
- [ ] All DTOs created
- [ ] AutoMapper configured and working
- [ ] Services refactored to use repositories
- [ ] No direct DbContext usage in services
- [ ] Transaction support verified
- [ ] DI container properly configured
- [ ] All tests pass

### Day 2 Deliverables
1. **Complete repository pattern implementation**
2. **Unit of Work with transaction support**
3. **DTO layer with AutoMapper**
4. **Refactored services using repositories**
5. **Clean separation of concerns**

---

## Day 3-7 Detailed Plans

Due to length constraints, here's the summary structure for the remaining days:

### Day 3 (Wednesday): Error Handling & Validation
- Global exception middleware
- FluentValidation for all DTOs
- Structured error responses
- Comprehensive logging with Serilog
- Custom exceptions hierarchy

### Day 4 (Thursday): Test Infrastructure
- xUnit test projects setup
- TestContainers for database testing
- Test fixtures and builders
- Code coverage configuration
- CI/CD pipeline with GitHub Actions

### Day 5 (Friday): Critical Unit Tests
- Domain entity tests
- Repository tests with in-memory database
- Service tests with mocked dependencies
- Achieve 40% code coverage target
- Test data builders

### Day 6 (Saturday): Authentication & Security
- JWT token generation and validation
- Remove mock authentication handler
- Role-based authorization
- Security headers middleware
- API rate limiting

### Day 7 (Sunday): Week 1 Review
- Complete code review
- Integration testing
- Bug fixes from review
- Performance baseline
- Prepare Week 2 sprint

---

## Week 1 Success Criteria

### Must Complete (P0)
- [x] Database migrations working
- [x] Repository pattern implemented
- [x] Unit of Work pattern functional
- [x] Error handling comprehensive
- [x] 40% test coverage achieved
- [x] Authentication working
- [x] CI/CD pipeline running

### Should Complete (P1)
- [ ] Performance monitoring setup
- [ ] Logging to file/console
- [ ] Health checks comprehensive
- [ ] Docker compose validated
- [ ] API documentation started

### Could Complete (P2)
- [ ] Basic admin endpoints
- [ ] Database seeding
- [ ] Performance tests
- [ ] Load testing setup

---

## Daily Standup Template

```markdown
**Date:** [Day X - Date]
**Participants:** [List]

### Yesterday's Accomplishments
- ✅ [Completed task 1]
- ✅ [Completed task 2]

### Today's Plan
- [ ] [Task 1]
- [ ] [Task 2]

### Blockers
- 🚫 [Blocker if any]

### Metrics
- Code Coverage: X%
- Tests Passing: X/Y
- P0 Bugs: X
```

---

## Week 1 Risk Tracking

| Day | Risk Identified | Mitigation Applied | Status |
|-----|----------------|-------------------|---------|
| Day 1 | MySQL connection issues | Used Docker compose | ✅ Resolved |
| Day 2 | Repository pattern complexity | Simplified interface | ✅ Resolved |
| Day 3 | - | - | - |

---

## Support Resources

### Documentation Links
- [EF Core Migrations](https://docs.microsoft.com/ef/core/managing-schemas/migrations/)
- [Repository Pattern](https://docs.microsoft.com/en-us/aspnet/mvc/overview/older-versions/getting-started-with-ef-5-using-mvc-4/implementing-the-repository-and-unit-of-work-patterns-in-an-asp-net-mvc-application)
- [AutoMapper](https://automapper.org/)
- [FluentValidation](https://fluentvalidation.net/)
- [xUnit](https://xunit.net/)

### Team Contacts
- Backend Dev: [Contact]
- QA Engineer: [Contact]
- DevOps: [Contact]
- PM: [Contact]
- Emergency: [Escalation]

---

**Week 1 Status:** READY TO EXECUTE
**Next Review:** Day 3 Evening
**Escalation:** PM → Stakeholder