using Eto.Forms;
using Eto.Drawing;
using Ropu.ClientUI.ViewModels;
using Ropu.Gui.Shared.ViewModels;

namespace Ropu.ClientUI.Views
{
    public class PttView : Panel
    {
        readonly PttPage _pttCircle;
        public PttView (PttViewModel<Color> pttViewModel, PttPage pttPage)
        {
            _pttCircle = pttPage;
            _pttCircle.BindDataContext(c => c.ButtonDownCommand, (PttViewModel<Color> model) => model.PttDownCommand);
            _pttCircle.BindDataContext(c => c.ButtonUpCommand, (PttViewModel<Color> model) => model.PttUpCommand);
            _pttCircle.PttColorBinding.BindDataContext<PttViewModel<Color>>(m => m.PttColor);

            _pttCircle.TalkerBinding.BindDataContext<PttViewModel<Color>>(m => m.Talker);
            _pttCircle.TalkerImageBinding.BindDataContext<PttViewModel<Color>>(m => m.TalkerImage);
            _pttCircle.IdleGroupBinding.BindDataContext<PttViewModel<Color>>(m => m.IdleGroup);
            _pttCircle.IdleGroupImageBinding.BindDataContext<PttViewModel<Color>>(m => m.IdleGroupImage);
            _pttCircle.CallGroupBinding.BindDataContext<PttViewModel<Color>>(m => m.CallGroup);
            _pttCircle.CallGroupImageBinding.BindDataContext<PttViewModel<Color>>(m => m.CallGroupImage);
            _pttCircle.CircleTextBinding.BindDataContext<PttViewModel<Color>>(m => m.CircleText);
            _pttCircle.TransmittingBinding.BindDataContext<PttViewModel<Color>>(m => m.Transmitting);
            _pttCircle.TransmittingAnimationColor = pttViewModel.Green;
            _pttCircle.ReceivingAnimationColor = pttViewModel.Red;

            Content = _pttCircle;
            DataContext = pttViewModel;

            this.Shown += async (sender, args) => 
            {
                await pttViewModel.Initialize();
            };
        }
    }
}
