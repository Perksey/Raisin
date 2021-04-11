using System.Threading.Tasks;
using RazorLight;

namespace Raisin.Core
{
    public abstract class RaisinPage<T> : TemplatePage<T>
    {
        public void ContentForModel(object model) => ContentForModelAsync(model).GetAwaiter().GetResult();

        public async Task ContentForModelAsync(object model)
        {
            
        }
    }
}