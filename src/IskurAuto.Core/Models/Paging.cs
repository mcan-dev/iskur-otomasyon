namespace IskurAuto.Core.Models;

/// <summary>Sayfalama isteği. Liste ekranlarının tüm tabloyu çekmesini önler.</summary>
public class PageRequest
{
    public const int MaxPageSize = 100;

    private int _page = 1;
    private int _pageSize = 20;

    /// <summary>1 tabanlı sayfa numarası.</summary>
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    /// <summary>Sayfa boyutu; 1 ile <see cref="MaxPageSize"/> arasına sıkıştırılır.</summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, 1, MaxPageSize);
    }

    public int Skip => (Page - 1) * PageSize;
}

/// <summary>Sayfalanmış sonuç.</summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>Aynı sayfalama bilgisiyle öğeleri başka tipe dönüştürür (entity → DTO).</summary>
    public PagedResult<TOut> Map<TOut>(Func<T, TOut> selector) => new()
    {
        Items = Items.Select(selector).ToList(),
        TotalCount = TotalCount,
        Page = Page,
        PageSize = PageSize
    };
}
