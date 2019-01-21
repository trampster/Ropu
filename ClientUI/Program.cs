using System;
using Eto.Forms;
using Eto.Drawing;

namespace ClientUI
{

    public class MyForm : Form
    {
        public MyForm ()
        {
            Title = "My Cross-Platform App";
            ClientSize = new Size(200, 200);
            Content = new Label { Text = "Hello World!" };
        }
        
        [STAThread]
        static void Main()
        {
            new Application().Run(new MyForm());
        }
    }
}
