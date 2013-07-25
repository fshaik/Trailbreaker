using System.Windows.Forms;

namespace Trailbreaker.MainApplication
{
    public abstract class TrailbreakerReceiverForm : Form
    {
        public abstract void AddAction(UserAction userAction);
        public abstract void AddCharacter(char c);
    }
}