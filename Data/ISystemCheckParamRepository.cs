using IQA_SOURCE.Models.Admin;

namespace IQA_SOURCE.Data
{
    public interface ISystemCheckParamRepository
    {
        Task<SystemCheckParamResponse> GetAllParams(string userId);
        Task<SystemCheckParamResponse> GetParamByCode(string paramCode, string userId);
        Task<SystemCheckParamResponse> UpsertParam(SystemCheckParam param, string userId);
        Task<SystemCheckSettings> GetResolvedSettings();
    }
}