using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace FP.MissingLink;

public class ServiceResponse<T>
{
    public HttpStatusCode StatusCode { get; set; }
    public string Reason { get; set; }
    public string ErrorMessage { get; set; }
    public T ApiResult { get; set; }
    public Dictionary<string, string> ResponseData { get; set; }
    public bool HasErrors => (int)StatusCode >= 400;
}

public class CaseDto
{
    public int Id { get; set; }
    [Required(AllowEmptyStrings = false)]
    [MaxLength(10)]
    public string Reference { get; set; }
    [Required(AllowEmptyStrings = false)]
    [MaxLength(80)]
    public string Name { get; set; }
    public string Description { get; set; }
    [MaxLength(260)] public string Folder { get; set; }
    public DateTime InstructionDate { get; set; }
}
public interface ICaseDataService
{
    Task<ServiceResponse<CaseDto>> GetByIdAsync(int id);
    Task<ServiceResponse<IEnumerable<CaseDto>>> CaseList(QueryFullParameters queryParameters);

}
public class CaseDataService : HttpHelperService, ICaseDataService
{

    public CaseDataService(HttpClient httpClientInstance)
        : base(httpClientInstance)
    {
        ControllerBasePath = "Case";
    }

    public async Task<ServiceResponse<IEnumerable<CaseDto>>> CaseList(QueryFullParameters queryParameters)
    {
        var response =
            await HttpClientInstance.GetAsync($"{ControllerBasePath}?{queryParameters.ToQueryString()}");
        var serviceResponse = await SetResponse<IEnumerable<CaseDto>>(response);
        if (!response.IsSuccessStatusCode) return serviceResponse;
        var obj = await response.Content.ReadAsStringAsync();
        serviceResponse.ApiResult = JsonConvert.DeserializeObject<IEnumerable<CaseDto>>(obj);
        serviceResponse.ResponseData = DecodeHeader(response);
        return serviceResponse;
    }
    public async Task<ServiceResponse<CaseDto>> GetByIdAsync(int id)
    {
        var message = CreateGetMessage($"{ControllerBasePath}/{id}", null);
        var response = await HttpClientInstance.SendAsync(message);
        var serviceResponse = await SetResponse<CaseDto>(response);
        if (response.IsSuccessStatusCode)
        {
            var obj = await response.Content.ReadAsStringAsync();
            var caseDto = JsonConvert.DeserializeObject<CaseDto>(obj);
            serviceResponse.ApiResult = caseDto;
        }
        return serviceResponse;
    }
}

public abstract class HttpHelperService
{
    private const string MediaTypeVersion = "application/json;v=1.0";
    protected readonly HttpClient HttpClientInstance;

    protected HttpHelperService(HttpClient httpClientFactory)
    {
        HttpClientInstance = httpClientFactory;
    }

    public string ControllerBasePath { get; set; }
    protected async Task<ServiceResponse<T>> SetResponse<T>(HttpResponseMessage response)
    {
        var serviceResponse = new ServiceResponse<T>
        {
            StatusCode = response.StatusCode,
            Reason = response.ReasonPhrase
        };
        if (!response.IsSuccessStatusCode)
        {
            serviceResponse.ErrorMessage = await response.Content.ReadAsStringAsync();
        }
        return serviceResponse;
    }

    protected HttpRequestMessage CreateGetMessage(string uriFragment, StringContent json)
    {
        var uri = new Uri(HttpClientInstance.BaseAddress + uriFragment);
        var msg = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = uri,
            Headers =
                {
                    {HttpRequestHeader.Accept.ToString(), MediaTypeVersion}
                },
            Content = json
        };
        return msg;
    }

    protected HttpRequestMessage CreatePostMessage(string uriFragment, StringContent json)
    {
        var uri = new Uri(HttpClientInstance.BaseAddress + uriFragment);
        var msg = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = uri,
            Headers =
                {
                    {HttpRequestHeader.ContentType.ToString(), MediaTypeVersion}
                },
            Content = json
        };
        return msg;
    }

    protected HttpRequestMessage CreatePutMessage(string uriFragment, StringContent json)
    {
        var uri = new Uri(HttpClientInstance.BaseAddress + uriFragment);
        var msg = new HttpRequestMessage
        {
            Method = HttpMethod.Put,
            RequestUri = uri,
            Headers =
                {
                    {HttpRequestHeader.ContentType.ToString(), MediaTypeVersion}
                },
            Content = json
        };
        return msg;
    }

    protected HttpRequestMessage CreateDeleteMessage(string uriFragment)
    {
        var uri = new Uri(HttpClientInstance.BaseAddress + uriFragment);
        var msg = new HttpRequestMessage
        {
            Method = HttpMethod.Delete,
            RequestUri = uri,
            Headers =
                {
                    {HttpRequestHeader.ContentType.ToString(), MediaTypeVersion}
                }
        };
        return msg;
    }

    public Dictionary<string, string> DecodeHeader(HttpResponseMessage response)
    {
        if (response.Headers.Contains("X-Pagination"))
        {
            return DecodePaginationHeader(response);
        }
        return response.Headers.Contains("X-QueryParameters") ? DecodeQueryParameterHeaders(response) : new Dictionary<string, string>();
    }

    private Dictionary<string, string> DecodePaginationHeader(HttpResponseMessage response)
    {
        var paginationValues = new Dictionary<string, string>();
        var paginationJson = new
        {
            totalCount = "",
            pageSize = "",
            currentPage = "",
            totalPages = "",
            prevPageLink = "",
            nextPageLink = ""
        };
        if (!response.Headers.TryGetValues("X-Pagination", out var headerValue)) return paginationValues;

        var headerValues = JsonConvert.DeserializeAnonymousType(headerValue.First(), paginationJson);
        paginationValues.Add("totalCount", headerValues.totalCount);
        paginationValues.Add("pageSize", headerValues.pageSize);
        paginationValues.Add("currentPage", headerValues.currentPage);
        paginationValues.Add("totalPages", headerValues.totalPages);
        paginationValues.Add("prevPageLink", headerValues.prevPageLink);
        paginationValues.Add("nextPageLink", headerValues.nextPageLink);
        return paginationValues;
    }

    private Dictionary<string, string> DecodeQueryParameterHeaders(HttpResponseMessage response)
    {
        var queryParameterValues = new Dictionary<string, string>();
        var queryParamJson = new
        {
            totalCount = "",
            pageSize = "",
            currentPage = "",
            totalPages = "",
            prevPageLink = "",
            nextPageLink = "",
            currentPageLink = "",
        };
        if (!response.Headers.TryGetValues("X-QueryParameters", out var headerValue)) return queryParameterValues;

        var headerValues = JsonConvert.DeserializeAnonymousType(headerValue.First(), queryParamJson);
        queryParameterValues.Add("totalCount", headerValues.totalCount);
        queryParameterValues.Add("pageSize", headerValues.pageSize);
        queryParameterValues.Add("currentPage", headerValues.currentPage);
        queryParameterValues.Add("totalPages", headerValues.totalPages);
        queryParameterValues.Add("prevPageLink", headerValues.prevPageLink);
        queryParameterValues.Add("nextPageLink", headerValues.nextPageLink);
        queryParameterValues.Add("currentPageLink", headerValues.currentPageLink);
        return queryParameterValues;
    }
}

public interface ITypedClientConfig
{
    Uri ApiUrl { get; set; }
    int HttpTimeout { get; set; }
    string ApiVersion { get; set; }
}
public class TypedClientConfig : ITypedClientConfig
{
    public TypedClientConfig(IOptions<HttpClientSettings> options)
    {
        HttpClientSettings settings = options.Value;
        ApiUrl = settings.ApiUrl;
        HttpTimeout = settings.HttpTimeout;
        ApiVersion = settings.ApiVersion;
    }

    #region Implementation of ITypedClientConfig

    public Uri ApiUrl { get; set; }
    public int HttpTimeout { get; set; }
    public string ApiVersion { get; set; }

    #endregion
}
public class HttpClientSettings
{
    public Uri ApiUrl { get; set; }

    public int HttpTimeout { get; set; }

    public string ApiVersion { get; set; }
}

public class QueryFullParameters
{
    const int MaxPageSize = 250;
    const int DefaultPageSize = 50;

    public QueryFullParameters()
    {
        SortCriteria = new Dictionary<string, bool>();
    }

    public int PageNumber { get; set; } = 1;

    private int _pageSize = DefaultPageSize;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = (value > MaxPageSize) ? MaxPageSize : (value < 1) ? DefaultPageSize : value;
    }
    public IDictionary<string, bool> SortCriteria { get; set; }
    public string ToQueryString()
    {
        StringBuilder queryString = new StringBuilder($"pageNumber={PageNumber}&pageSize={PageSize}");
        if (SortCriteria.Count > 0)
        {
            queryString.Append("&orderBy=");
            foreach (var criterion in SortCriteria)
            {
                queryString.Append($"{criterion.Key}{(criterion.Value ? " desc" : "")},");
            }
        }
        return queryString.ToString();
    }
}

