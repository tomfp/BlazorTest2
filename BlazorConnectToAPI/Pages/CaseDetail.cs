using FP.MissingLink;
using Microsoft.AspNetCore.Components;
//using FP.Domain.Enum;
//using FP.ViewModel.Api.Data;
//using FP.ViewModel.Dto2;

namespace BlazorConnectToAPI.Pages;

public partial class CaseDetail
{
    [Parameter]
    public string FpCaseId { get; set; }
    [Inject]
    public ICaseDataService CaseDataService { get; set; }

    [Inject]
    public ILogger<CaseDetail> Logger { get; set; }

    public CaseDto Case { get; set; }

    public string ApiError { get; set; }


    #region Overrides of ComponentBase

    /// <summary>
    /// Method invoked when the component is ready to start, having received its
    /// initial parameters from its parent in the render tree.
    /// Override this method if you will perform an asynchronous operation and
    /// want the component to refresh when that operation is completed.
    /// </summary>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> representing any asynchronous operation.</returns>
    protected override async Task OnInitializedAsync()
    {
        await InitialiseCase();
    }

    #endregion

    private async Task InitialiseCase()
    {
        try
        {
            if (int.TryParse(FpCaseId, out var caseId))
            {
                var serviceResponse = await CaseDataService.GetByIdAsync(caseId);
                if (!serviceResponse.HasErrors)
                {
                    Case = serviceResponse.ApiResult;
                }
                else
                {
                    ApiError = $"Error calling API: {serviceResponse.ErrorMessage} {serviceResponse.Reason}";
                    Logger.LogError($"Unknown Case Id {caseId} API returned {serviceResponse.StatusCode} {serviceResponse.Reason}");
                }
            }
            else
            {
                ApiError = $"Invalid Case Id {FpCaseId}";
            }
        }
        catch (Exception ex)
        {
            ApiError += $"Error loading cases: {ex.Message} {ex.StackTrace}";
            Logger.LogError(ex, "API Error");
        }
    }
}
