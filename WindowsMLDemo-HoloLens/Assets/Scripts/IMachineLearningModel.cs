using System.Threading.Tasks;
#if WINDOWS_UWP
using Windows.AI.MachineLearning.Preview;
#endif
namespace WindowsMLDemos.Common
{
    public interface IMachineLearningModel
    {
#if WINDOWS_UWP
        LearningModelPreview LearningModel { get; set; }

        Task<IMachineLearningOutput> EvaluateAsync(IMachineLearningInput input);
#endif
    }
}
