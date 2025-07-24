using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace youtube_trivia_bot

{

    public partial class Form1 : Form
    {
        private TriviaBot bot;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            bot = new TriviaBot();
            bot.Log = message => Invoke((Action)(() => logTextBox.AppendText(message + Environment.NewLine)));

            bot.Start();

            button1.Enabled = false;
            button2.Enabled = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            bot?.Disconnect();
            button1.Enabled = true;
            button2.Enabled = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void logTextBox_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
