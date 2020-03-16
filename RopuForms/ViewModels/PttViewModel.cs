using System.Threading.Tasks;
using RopuForms.Services;
using Xamarin.Forms;

namespace RopuForms.ViewModels
{
    public class PttViewModel : BaseViewModel
    {
        public Command PttDownCommand { get; set; }
        public Command PttUpCommand { get; set; }

        public PttViewModel()
        {
            PttDownCommand = new Command(() => ExecutePttDown());
            PttUpCommand = new Command(() => ExecutePttUp());
        }

        void ExecutePttDown()
        {
            PttColor = Green;
            Transmitting = true;
        }

        void ExecutePttUp()
        {
            PttColor = Blue;
            Transmitting = false;
        }

        public readonly static Color Blue = Color.FromRgb(0x31,0x93, 0xe3);

        public readonly static Color Green = Color.FromRgb(0x31, 0xe3, 0x93);

        public readonly static Color Gray = Color.FromRgb(0x99, 0x99, 0x99);

        public readonly static Color Red = Color.FromRgb(0xFF, 0x69, 0x61);

        Color _pttColor = Blue;
        public Color PttColor
        {
            get => _pttColor;
            set => SetProperty(ref _pttColor, value);
        }

        Color _transmittingColor = Green;
        public Color TransmittingColor
        {
            get => _transmittingColor;
            set => SetProperty(ref _transmittingColor, value);
        }

        bool _transmitting = false;
        public bool Transmitting
        {
            get => _transmitting;
            set => SetProperty(ref _transmitting, value);
        }

    }
}
