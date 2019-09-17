using Eto.Forms;
using Eto.Drawing;
using Ropu.ClientUI.ViewModels;

namespace Ropu.ClientUI.Views
{
    public class PttView : Panel
    {
        readonly PttPage _pttCircle;
        public PttView (PttViewModel mainViewModel, PttPage pttPage)
        {
            _pttCircle = pttPage;
            _pttCircle.BindDataContext(c => c.ButtonDownCommand, (PttViewModel model) => model.PttDownCommand);
            _pttCircle.BindDataContext(c => c.ButtonUpCommand, (PttViewModel model) => model.PttUpCommand);
            _pttCircle.PttColorBinding.BindDataContext<PttViewModel>(m => m.PttColor);

            _pttCircle.TalkerBinding.BindDataContext<PttViewModel>(m => m.Talker);
            _pttCircle.TalkerImageBinding.BindDataContext<PttViewModel>(m => m.TalkerImage);
            _pttCircle.IdleGroupBinding.BindDataContext<PttViewModel>(m => m.IdleGroup);
            _pttCircle.IdleGroupImageBinding.BindDataContext<PttViewModel>(m => m.IdleGroupImage);
            _pttCircle.CallGroupBinding.BindDataContext<PttViewModel>(m => m.CallGroup);
            _pttCircle.CallGroupImageBinding.BindDataContext<PttViewModel>(m => m.CallGroupImage);
            _pttCircle.CircleTextBinding.BindDataContext<PttViewModel>(m => m.CircleText);
            _pttCircle.TransmittingBinding.BindDataContext<PttViewModel>(m => m.Transmitting);
            _pttCircle.TransmittingAnimationColor = PttViewModel.Green;
            _pttCircle.ReceivingAnimationColor = PttViewModel.Red;

            Content = _pttCircle;
            DataContext = mainViewModel;

            this.Shown += async (sender, args) => 
            {
                await mainViewModel.Initialize();
            };
        }
    }
}
