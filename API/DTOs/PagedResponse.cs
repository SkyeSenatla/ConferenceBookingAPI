namespace API.DTOs; 

public record PagedResponse<T> 
(
    IEnumerable<T> Data, 
    int Page, 
    int PageSize, 
    int TotalPages,
    int TotalCount, 
    bool HasNextPage, 
    bool HasPreviousPage
); 