using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AudioSpectrumAdvance
{

    public partial class Audioly : Form
    {
        public Audioly()
        {
            InitializeComponent();

            Analyzer.high_low = 1;
            analyzer = new Analyzer(progressBar1, progressBar2, spectrum1, comboBox1, chart1, groupBox1, radioButton1, radioButton2, radioButton3, radioButton4, radioButton5, radioButton6, radioButton7, radioButton8);
            analyzer.Enable = true;
            analyzer.DisplayEnable = true;

            timer1.Enabled = true;
        }
        Analyzer analyzer;

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void timer1_Tick(object sender, EventArgs e)
        {




        }

        private void chart2_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Analyzer.high_low = 1;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Analyzer.high_low = 2;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Analyzer.high_low = 3;
        }

    }
}
