using AutoMapper;
using ProductService.DTOs;
using ProductService.Models;
using ProductService.Repositories;

namespace ProductService.Services;

public class ProductServiceImpl : IProductService
{
    private readonly IProductRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductServiceImpl> _logger;

    public ProductServiceImpl(IProductRepository repository, IMapper mapper, ILogger<ProductServiceImpl> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
    {
        var products = await _repository.GetAllAsync();
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product == null)
        {
            _logger.LogWarning("Product with ID {ProductId} not found", id);
            return null;
        }
        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto dto)
    {
        var product = _mapper.Map<Product>(dto);
        var created = await _repository.CreateAsync(product);
        _logger.LogInformation("Product created successfully with ID: {ProductId}", created.Id);
        return _mapper.Map<ProductDto>(created);
    }

    public async Task<ProductDto?> UpdateProductAsync(int id, UpdateProductDto dto)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
        {
            _logger.LogWarning("Cannot update - Product with ID {ProductId} not found", id);
            return null;
        }

        if (dto.Name != null) existing.Name = dto.Name;
        if (dto.Price.HasValue) existing.Price = dto.Price.Value;
        if (dto.Stock.HasValue) existing.Stock = dto.Stock.Value;

        var updated = await _repository.UpdateAsync(existing);
        return updated != null ? _mapper.Map<ProductDto>(updated) : null;
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        return await _repository.DeleteAsync(id);
    }
}
