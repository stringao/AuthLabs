using Microsoft.EntityFrameworkCore;
using Xunit;
using AuthLabs.Resource.Data;
using AuthLabs.Resource.Models;
using AuthLabs.Resource.Services;

namespace AuthLabs.Resource.Tests.Services;

public class DocumentServiceTests : IDisposable
{
    private readonly ResourceDbContext _context;
    private readonly DocumentService _service;

    public DocumentServiceTests()
    {
        var options = new DbContextOptionsBuilder<ResourceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ResourceDbContext(options);
        _service = new DocumentService(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region CanUserEditAsync Tests

    [Fact]
    public async Task CanUserEditAsync_AsOwner_ReturnsTrue()
    {
        // Arrange
        var document = new Document
        {
            Id = 1,
            Title = "Test Document",
            OwnerId = "owner-123",
            Permissions = new List<DocumentPermission>()
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CanUserEditAsync(1, "owner-123");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanUserEditAsync_WithEditPermission_ReturnsTrue()
    {
        // Arrange
        var document = new Document
        {
            Id = 2,
            Title = "Test Document",
            OwnerId = "owner-456",
            Permissions = new List<DocumentPermission>
            {
                new() { UserId = "editor-789", CanEdit = true, CanDelete = false }
            }
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CanUserEditAsync(2, "editor-789");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanUserEditAsync_WithOnlyDeletePermission_ReturnsFalse()
    {
        // Arrange
        var document = new Document
        {
            Id = 3,
            Title = "Test Document",
            OwnerId = "owner-abc",
            Permissions = new List<DocumentPermission>
            {
                new() { UserId = "delete-only-user", CanEdit = false, CanDelete = true }
            }
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CanUserEditAsync(3, "delete-only-user");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanUserEditAsync_WithNoPermission_ReturnsFalse()
    {
        // Arrange
        var document = new Document
        {
            Id = 4,
            Title = "Test Document",
            OwnerId = "owner-xyz",
            Permissions = new List<DocumentPermission>()
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CanUserEditAsync(4, "random-user");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanUserEditAsync_DocumentDoesNotExist_ReturnsFalse()
    {
        // Act
        var result = await _service.CanUserEditAsync(999, "any-user");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanUserEditAsync_UserHasPermissionButCanEditIsFalse_ReturnsFalse()
    {
        // Arrange
        var document = new Document
        {
            Id = 5,
            Title = "Test Document",
            OwnerId = "owner-123",
            Permissions = new List<DocumentPermission>
            {
                new() { UserId = "user-without-edit", CanEdit = false, CanDelete = false }
            }
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CanUserEditAsync(5, "user-without-edit");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region CanUserDeleteAsync Tests

    [Fact]
    public async Task CanUserDeleteAsync_AsOwner_ReturnsTrue()
    {
        // Arrange
        var document = new Document
        {
            Id = 10,
            Title = "Test Document",
            OwnerId = "owner-123",
            Permissions = new List<DocumentPermission>()
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CanUserDeleteAsync(10, "owner-123");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanUserDeleteAsync_WithDeletePermission_ReturnsTrue()
    {
        // Arrange
        var document = new Document
        {
            Id = 11,
            Title = "Test Document",
            OwnerId = "owner-456",
            Permissions = new List<DocumentPermission>
            {
                new() { UserId = "deleter-789", CanEdit = false, CanDelete = true }
            }
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CanUserDeleteAsync(11, "deleter-789");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanUserDeleteAsync_WithEditPermissionOnly_ReturnsFalse()
    {
        // Arrange
        var document = new Document
        {
            Id = 12,
            Title = "Test Document",
            OwnerId = "owner-abc",
            Permissions = new List<DocumentPermission>
            {
                new() { UserId = "edit-only-user", CanEdit = true, CanDelete = false }
            }
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CanUserDeleteAsync(12, "edit-only-user");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanUserDeleteAsync_WithNoPermission_ReturnsFalse()
    {
        // Arrange
        var document = new Document
        {
            Id = 13,
            Title = "Test Document",
            OwnerId = "owner-xyz",
            Permissions = new List<DocumentPermission>()
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CanUserDeleteAsync(13, "random-user");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanUserDeleteAsync_DocumentDoesNotExist_ReturnsFalse()
    {
        // Act
        var result = await _service.CanUserDeleteAsync(999, "any-user");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanUserDeleteAsync_UserHasPermissionButCanDeleteIsFalse_ReturnsFalse()
    {
        // Arrange
        var document = new Document
        {
            Id = 14,
            Title = "Test Document",
            OwnerId = "owner-123",
            Permissions = new List<DocumentPermission>
            {
                new() { UserId = "user-without-delete", CanEdit = true, CanDelete = false }
            }
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CanUserDeleteAsync(14, "user-without-delete");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region CRUD Tests (supporting tests)

    [Fact]
    public async Task GetDocumentByIdAsync_ReturnsDocumentWithPermissions()
    {
        // Arrange
        var document = new Document
        {
            Id = 100,
            Title = "Test Document",
            OwnerId = "owner-100",
            Permissions = new List<DocumentPermission>
            {
                new() { UserId = "user-100", CanEdit = true, CanDelete = true }
            }
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDocumentByIdAsync(100);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Document", result.Title);
        Assert.Single(result.Permissions);
    }

    [Fact]
    public async Task CreateDocumentAsync_CreatesDocument()
    {
        // Arrange
        var document = new Document
        {
            Title = "New Document",
            Content = "New Content",
            OwnerId = "new-owner"
        };

        // Act
        var result = await _service.CreateDocumentAsync(document);

        // Assert
        Assert.True(result.Id > 0);
        var saved = await _context.Documents.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("New Document", saved.Title);
    }

    [Fact]
    public async Task DeleteDocumentAsync_DeletesExistingDocument_ReturnsTrue()
    {
        // Arrange
        var document = new Document
        {
            Id = 200,
            Title = "To Delete",
            OwnerId = "owner-200"
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteDocumentAsync(200);

        // Assert
        Assert.True(result);
        var deleted = await _context.Documents.FindAsync(200);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteDocumentAsync_DocumentDoesNotExist_ReturnsFalse()
    {
        // Act
        var result = await _service.DeleteDocumentAsync(999);

        // Assert
        Assert.False(result);
    }

    #endregion
}
