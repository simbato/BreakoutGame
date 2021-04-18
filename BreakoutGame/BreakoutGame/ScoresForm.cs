using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BreakoutGame
{
    public partial class ScoresForm : Form
    {

        List<Label> labels = new List<Label>();
        public ScoresForm(int score, string name = "", int k = 0)
        {
            InitializeComponent();

            labels.Add(label1);
            labels.Add(label2);
            labels.Add(label3);
            labels.Add(label4);
            labels.Add(label5);
            labels.Add(label6);

            
            
            // ako je name = "" znaci da nije postignut novi high score
            if (name == "")
            {
                var stream = new StreamReader(@".\..\..\Resources\highScore.txt");
                for (int i = 0; i < 6; i += 2)
                {
                    string red = stream.ReadLine();
                    string[] names = red.Split(',');
                    labels[i].Text = names[1];
                    labels[i + 1].Text = names[0];
                }

                label8.Text = "Vaš rezultat: " + score.ToString();
            }
            else    //inace znaci da treba upisati novi highscore
            {
                label8.Text = "Čestitamo!";
                var stream = new StreamReader(@".\..\..\Resources\highScore.txt");
                List<string> za_upisati = new List<string>();
                for (int i = 0; i < 6; i += 2)
                {
                    if (i/2 == k)
                    {
                        //upisi name i score i dodaj ih u liste
                        labels[i].Text = name;
                        labels[i].ForeColor = Color.DarkGreen;
                        labels[i + 1].ForeColor = Color.DarkGreen;
                        labels[i + 1].Text = score.ToString();
                        za_upisati.Add(score.ToString());
                        za_upisati.Add(name);
                    }
                    else
                    {
                        string red = stream.ReadLine();
                        string[] names = red.Split(',');
                        labels[i].Text = names[1];
                        labels[i + 1].Text = names[0];

                        //dodaj u listu za upisivanje
                        za_upisati.Add(names[0]);
                        za_upisati.Add(names[1]);
                    }
                }
                stream.Close();

                //writer1 sluzi da obrisemo sve iz highScore.txt
                var writer1 = new StreamWriter(@".\..\..\Resources\highScore.txt", false);
                writer1.WriteLine("");
                writer1.Close();

                //zapisi sve iz liste u highScore.txt
                using (StreamWriter writer = new StreamWriter(@".\..\..\Resources\highScore.txt"))
                {
                    for (int i = 0; i < 3; i++)
                    {
                        string line = za_upisati[i * 2] + "," + za_upisati[i * 2 + 1];
                        writer.WriteLine(line);
                    }
                }
                
            }
          
        }



        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click_1(object sender, EventArgs e)
        {

        }


        private void ScoresForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Enter)
                this.Close();
        }
    }
}
