using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.IO;

namespace BreakoutGame
{
	public partial class Form1 : Form
	{
		// Gledaju krecemo li se lijevo ili desno s igracem
		bool goLeft;	
		bool goRight;

		//true ako je igra gotova
		bool isGameOver;

		int score;				//pratimo trenutni rezultat
		int playerSpeed;		//Brzina pomaka ploce

		int counter;            //broji vrijeme igre
		int time_to_shift;      //broji vrijeme do pomaka kocki prema dolje	
		int mSeconds;			//sluzi za pracenje minuta provedenih u igri kako bi mogli postrepeno ubrzavati lopticu

		double ball_speed;      //trenutna brzina loptice
		double standard_ball_speed;  //uvijek standardna brzina
		int fast_slow_time;			//mjeri trajanje efekta (efekti trebaju trajat 10 sekundi)

		int lowest;             //prati poziciju najdonje kocke

		//Ove varijable sluze za pokretanje loptice. Pomicemo ju tako da
		//poziciji loptice dodamo ballX s lijeve, odnosno ballY s gornje strane.
		//Uvijek vrijedi ballX^2 + ballY^2 = ball_speed^2
		
		//Postavljamo stvari za situaciju s vise loptica u igri. Umjesto jedne loptice, imat cemo niz loptica
		//(najcesce ce u nizu biti samo jedna loptica, osim kada pokupimo efekt za vise njih).
		List<double> ballXList = new List<double>();
		List<double> ballYList = new List<double>();
		List<PictureBox> ballList = new List<PictureBox>();

		List<PictureBox> blockList = new List<PictureBox>();

		//Lista posebnih efekata. Ukljucuje staticke(destroy, newBall) i padajuce efekte(+50, +100, fast, slow).
		List<PictureBox> effectList = new List<PictureBox>();

		Random rnd = new Random();


		//Zvukovi:
		//SoundPlayer moze pustati samo jedan zvuk istovremeno i to radi
		//u zasebnoj dretvi tako da se javlja bug kad se razbije vise cigli odjednom.
		System.Media.SoundPlayer explosionSound = new System.Media.SoundPlayer(Properties.Resources.sound_explosion);
		System.Media.SoundPlayer brickSound = new System.Media.SoundPlayer(Properties.Resources.sound_brick_collision2);
		System.Media.SoundPlayer gameoverSound = new System.Media.SoundPlayer(Properties.Resources.sound_gameover);
		System.Media.SoundPlayer pointsSound = new System.Media.SoundPlayer(Properties.Resources.sound_points_earned);
		System.Media.SoundPlayer startSound = new System.Media.SoundPlayer(Properties.Resources.sound_start);
		System.Media.SoundPlayer wallSound = new System.Media.SoundPlayer(Properties.Resources.sound_wall);


		//da znamo u kojem trenutku je koji zvuk pusten
		DateTime startTime = DateTime.Now;

		string last_sound_type;
		double last_sound_time;

		private void playSound(string s) {
			DateTime currentTime = DateTime.Now;
			TimeSpan elapsed_time = currentTime - startTime;
			double time_sec = elapsed_time.TotalSeconds;
			if (s != "gameover" && s != "explosion") { 
				//niti jedan zvuk, osim gameovera i nje same ne smije
				//prekidati eksploziju
				if (last_sound_type == "explosion" && time_sec - last_sound_time < 1.4)
					return;
				//osiguravamo se da za jednu koliziju nemamo vise od jednog zvuka,
				//a i smanjujemo buku ovime
				if (last_sound_type == s && time_sec - last_sound_time < 0.05)
					return;
				if (last_sound_type == "brick" && time_sec - last_sound_time < 0.2)
					return;


			}

			//ako smo dosli do ovog dijela znaci da ce se pokrenuti novi zvuk
			last_sound_type = s;
			last_sound_time = time_sec;
			if (s == "explosion")
				explosionSound.Play();
			else if (s == "brick")
				brickSound.Play();
			else if (s == "gameover")
				gameoverSound.Play();
			else if (s == "points")
				pointsSound.Play();
			else if (s == "start")
				startSound.Play();
			else if (s == "wall")
				wallSound.Play();

		}


		public Form1()
		{
			InitializeComponent();

			placeBlocks();

		}

		private void setupGame() 
		{
			isGameOver = false;
			score = 0;
			counter = 0;
			mSeconds = 0;
			lowest = 0;
			time_to_shift = 0;
			fast_slow_time = 0;
			playerSpeed = 16;
			scoreText.Text = "Score: " + score;
			textBox1.Text = "CLICK where you want to send the ball";
			label2.Text = "00:00";

			goLeft = false;
			goRight = false;


			//namjesti plocu na sredinu
			player.Left = (int)(splitContainer1.Panel2.Width / 2 - player.Width / 2);

			//na pocetku je loptica nepomicna, tj. stoji na ploci
			ball_speed = 0;
			standard_ball_speed = 0;
			
			//Sve loptce ce imati istu brzinu u svakom trenutku.
			//Stvaramo pocetnu lopticu.
			var ballFirst = new PictureBox();
			ballFirst.Height = 26;
			ballFirst.Width = 26;
			ballFirst.BackColor = SystemColors.ControlDarkDark;
			//Smjestamo lopticu na sredinu igrace ploce.
			ballFirst.Left = player.Left + player.Width / 2 - ballFirst.Width / 2;
			ballFirst.Top = player.Top - ballFirst.Height;
			ballFirst.BackgroundImage = Properties.Resources.circle_cropped;
			ballFirst.BackgroundImageLayout = ImageLayout.Stretch;
			ballList.Add(ballFirst);
			this.splitContainer1.Panel2.Controls.Add(ballFirst);
			ballXList.Add(0);
			ballYList.Add(0);

			startTime = DateTime.Now;
			last_sound_type = "";
			last_sound_time = 0;

			gameTimer.Start();
		}

		private void placeBlocks()
		{
			// za pocetak nacrtaj 3 reda
			draw_rows(3);
			//pripremi igru
			setupGame();
		}
		private void draw_rows(int n) //crta n redova kocki, u svakom redu uvijek 10 kocki
        {
			//koordinate
			int top = 5;
			int left = 1;
			
			int width = (int)(splitContainer1.Panel2.Width - 14) / 10;

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < 10; j++)
                {
					//stvori blok i postavi mu svojstva
					var block = new PictureBox();
					block.Height = 32;
					block.Width = width;
					block.Left = left;
					block.Top = top;

					block.BackColor = Color.Gray;
					/*
					 * Crvena cigla: obicna cigla koja puca nakon prvog udarca. 
					 * Zuta cigla: obicna cigla koja puca nakon prvog udarca.
					 * Tamnozelena cigla: nakon prvog udarca napukne, nakog drugog skroz puca. Nosi vise bodova.
					 * Purpurna cigla: kad se razbija dogodi se neki padajuci efekt - fast, slow, +50, +100... 
					 * Destroy cigla - unisti svoje susjede
					 * NewBall cigla - stvara neki broj novih loptica.
					 * 
					 * Uzimamo random broj izmedu 0 i 1. Ako je u intervalu [0, 0.20] onda stvaramo 
					 * zutu ciglu, ako je u <0.20, 0.40] onda crvenu, ako je u <0.40, 0.60] stvaramo 
					 * tamnozelenu, a inace (<0.60, 0.80]) purpurnu. Za <0.80, 0.90] Destroy cigla, a
					 * <0.90, 1] NewBall cigla.
					 * Korigirat ove brojeve u testnoj fazi.
					 * 
					 * Ovi brojevi mozemo promijenjeni radi testiranja.
					*/

					double odluka_boje = rnd.NextDouble();
					bool is_effect = false;
					if(odluka_boje <= 0.20)
                    {
						block.BackgroundImage = Properties.Resources.yellowBrick;
						block.Tag = new Block { blockColor = "yellow" };
					}
					else if (odluka_boje <= 0.40) //0.40
                    {
						block.BackgroundImage = Properties.Resources.redBrick2;
						block.Tag = new Block { blockColor = "red" };
					}
					else if(odluka_boje <= 0.60) //0.60
                    {
						block.BackgroundImage = Properties.Resources.darkGreenBrick;
						block.Tag = new Block { blockColor = "darkGreen" };

					}
					else if(odluka_boje <= 0.80) // 0.80
                    {
						block.BackgroundImage = Properties.Resources.purpleBrick;
						block.Tag = new Block { blockColor = "purple" };

					}
					else if (odluka_boje <= 0.90) //0.90
                    {
						block.BackgroundImage = Properties.Resources.destroy;
						block.Tag = new Effect { Mobile = false, Description = "destroy" };
						is_effect = true;
					}
                    else
                    {
						block.BackgroundImage = Properties.Resources.newBall;
						block.Tag = new Effect { Mobile = false, Description = "newBall" };
						is_effect = true;
					}

					block.BackgroundImageLayout = ImageLayout.Stretch;
					
					//Malo nezgodna notacija "block". Ako je nastao efekt newBall ili desroy spremamo
					//u listu efekata.
					if (!is_effect)
						blockList.Add(block);
					else 
						effectList.Add(block);

					this.splitContainer1.Panel2.Controls.Add(block);

					left += width;
				}
				left = 1;
				top += 33;
				if (top > lowest)
					lowest = top;
            }
		}

		private void removeBlocks()
		{
			// Uklanja sve cigle
			foreach(PictureBox x in blockList)
			{
				this.splitContainer1.Panel2.Controls.Remove(x);
			}
			blockList.Clear();
			foreach (PictureBox x in effectList)
			{
				this.splitContainer1.Panel2.Controls.Remove(x);
			}
			effectList.Clear();
		}

		private void Form1_Load(object sender, EventArgs e)
		{

		}

		private void gameOver()
		{
			//sviraj zvuk i zaustavi timere
			playSound("broken");
			isGameOver = true;
			gameTimer.Stop();
			timer1.Stop();
			scoreText.Text = "Score: " + score;
			textBox1.Text = "Game over! Press Enter to play again.";
			playSound("gameover");

			//Ako je score među top 3, upiši ga u highScore.txt
			var stream = new StreamReader(@".\..\..\Resources\highScore.txt");
			
			//sluzi da znamo je li postignut novi highscore
			bool flag = false;

			for (int i = 0; i < 3; i++)
			{
				// citamo linije iz highScores.txt te usporedujemo sa score
				string red = stream.ReadLine();
				string[] names = red.Split(',');
				if (Int32.Parse(names[0]) < score)
                {
					// score treba upisati u listu highScore
					string ime = Prompt.ShowDialog("Osvojili ste "+ (i+1).ToString() + ". najbolji rezultat. Molimo upišite svoje ime.", "Čestitamo!");
					while (ime == "")
						ime = Prompt.ShowDialog("Molimo upišite ime.","Ne zafrkavajte se");
					stream.Close();
					Form highscores = new ScoresForm(score, ime, i);
					highscores.ShowDialog();
					flag = true;
					break;
				}
			}
			if (!flag)
            {
				//highScore nije psotignut
				Form highscores = new ScoresForm(score);
				highscores.ShowDialog();
			}
		}

		private void mainGameTimerEvent(object sender, EventArgs e) //glavni timer, ima interval 20 ms
		{
			scoreText.Text = "Score: " + score;

			//pomakni plocu ako je pritisnuta tipka za lijevo ili desno
			if (goLeft == true && player.Left > 0)
			{
				player.Left -= playerSpeed;
				if (ball_speed == 0.0)
                {
					for (int i = 0; i < ballList.Count; ++i)
						ballList[i].Left -= playerSpeed;
				}
			}
			if (goRight == true && player.Right < splitContainer1.Panel2.Width - 7) 
			{
				player.Left += playerSpeed;
				if (ball_speed == 0.0)
                {
					for (int i = 0; i < ballList.Count; ++i)
						ballList[i].Left += playerSpeed;
				}
					
			}

			//Pomicemo sve loptice na ploci
            for(int tmpCounter = 0; tmpCounter < ballList.Count; ++tmpCounter)
            {
				PictureBox mBall = ballList[tmpCounter];

				mBall.Left += (int)ballXList[tmpCounter];
				mBall.Top += (int)ballYList[tmpCounter];
            }

			//Lopta udara u rub prozora
			provjeriUdaranjeOdRub();

			//Pomicemo padajuce efekte. Dodati slucaj kada udari u igracevu plocu. 
			//Loptica samo prolazi kroz efekte.
			foreach (PictureBox ef in effectList.ToList())
			{
				Effect effectTag = (Effect)ef.Tag;
				//Imamo dvije vrste efekata. Staticni su oni koji stoje na pozicijama kao cigle (destroy i newBall).
				//Padajuci efekti su bonusi na bodove, te usporavanje i ubrzavanje loptice.
				if (!effectTag.Mobile) continue;
				
				ef.Top += 10;
				if (ef.Top + 10 > player.Top)
				{
					this.splitContainer1.Panel2.Controls.Remove(ef);
					effectList.Remove(ef);
				}
				else if (ef.Bounds.IntersectsWith(player.Bounds))
				{
					playSound("points"); //zapravo se misli na bonuse opcenito
					// Igrac je pokupio efekt
					// Imamo 4 slučaja:
					if (effectTag.Description == "bonus50")
					{
						score += 50;
						this.splitContainer1.Panel2.Controls.Remove(ef);
						effectList.Remove(ef);
					}
					else if (effectTag.Description == "bonus100")
					{
						score += 100;
						this.splitContainer1.Panel2.Controls.Remove(ef);
						effectList.Remove(ef);
					}
					else if (effectTag.Description == "fast")
					{
						//postavi fast_slow_time tako da se pokrene timer1
						//treba namistiti ballX i ballY tako da daju ball_speed^2
						//ali tu treba paziti da kut ostane isti
						fast_slow_time = 1;
						ball_speed = (int)(1.25 * standard_ball_speed);
						this.splitContainer1.Panel2.Controls.Remove(ef);
						effectList.Remove(ef);

						//promijeni ballX i ballY
						adjustBallSpeed();
		
					}
					else if (effectTag.Description == "slow")
					{
						fast_slow_time = 1;
						ball_speed = (int)(0.75 * standard_ball_speed);
						this.splitContainer1.Panel2.Controls.Remove(ef);
						effectList.Remove(ef);

						//promijeni ballX i ballY
						adjustBallSpeed();
					}
				}
			}

			//Lopta udara u igraca
			provjeriUdaranjeOdIgraca();

			// Provjeri dodiruje li lopta neku ciglu
			provjeriUdarenjeOdCiglu();

			//Provjera je li neka od loptica izasla iz granica polja.
			//Moramo ici u obrnutom smjeru zbog brisanja na odgovarajucim indeksima u listama ballXList i ballYList.
			//Inace bi obrisali manji indeks pa u iducem brisanju utjecali na pogresan element.
			for(int i = ballList.Count - 1; i >= 0; --i)
            {
				PictureBox mBall = ballList[i];
				if (mBall.Top > player.Top)
				{
					//Izbacujemo neku od loptica.
					ballList.Remove(mBall);
					this.splitContainer1.Panel2.Controls.Remove(mBall);
					ballXList.RemoveAt(i);
					ballYList.RemoveAt(i);
				}
			}
			//Ako su sve loptice izasle onda je kraj igre.
			if(ballList.Count == 0)
            {
				gameOver();
            }

			//Ako je proslo bar 10 sekundi od zadnjeg dodavanja probaj dodat novi red na vrh.
			if (time_to_shift > 10)
			{
				//prvo provjeri moze li se pomaknuti 
				foreach (var x in blockList)
				{
					for(int i = 0; i < ballList.Count; ++i)
						if (ballList[i].Bounds.IntersectsWith(x.Bounds))
							return;

				}
				foreach (var x in effectList)
				{
					Effect effectTag = (Effect)x.Tag;
					for(int i = 0; i < ballList.Count; ++i)
						if ( !effectTag.Mobile &&  ballList[i].Bounds.IntersectsWith(x.Bounds))
							return;

				}

				//ako je doslo do tu znaci da se moze pomaknuti
				foreach (var x in blockList)
				{
					x.Top += 33;
					lowest += 33;
					//ako su cigle dosle prenisko igra je gotova
					if (x.Top + 33 > player.Top)
						gameOver();
				}
				foreach (var x in effectList)
				{
					Effect effectTag = (Effect)x.Tag;
                    if (!effectTag.Mobile)
                    {
						x.Top += 33;
						lowest += 33;
						if (x.Top + 33 > player.Top)
							gameOver();
					}
					
				}
				draw_rows(1);
				//resetiraj brojac
				time_to_shift = 0;
			}
		}

		// provjeravamo je li se loptica sudarila s nekom ciglom
		private void provjeriUdarenjeOdCiglu()
		{
			for (int tmpCounter = 0; tmpCounter < ballList.Count; ++tmpCounter)
			{
				PictureBox mBall = ballList[tmpCounter];

				foreach (Control x in this.splitContainer1.Panel2.Controls)
				{
					//Gledamo presjek lopte s ciglama.
					if (mBall.Bounds.IntersectsWith(x.Bounds) && x is PictureBox)
					{
						if (x.Tag is Block || (x.Tag is Effect && !((Effect)x.Tag).Mobile))
						{
							bool dvijeOdjednom = false;
							PictureBox y = new PictureBox();
							// provjerimo je li imamo presjek s dvije cigle odjednom
							foreach (Control z in this.splitContainer1.Panel2.Controls)
							{
								if (mBall.Bounds.IntersectsWith(z.Bounds) && z is PictureBox)
								{
									if (z.Tag is Block || (z.Tag is Effect && !((Effect)z.Tag).Mobile))
									{
										if (z.Left == x.Left && z.Top == x.Top)
											continue;
										else
										{
											dvijeOdjednom = true;
											y = (PictureBox)z;
											break;
										}
									}
								}

							}
							//Loptica se obija i od statickih efekata jednako kao i od cigli.
							// 1. slučaj imamo presjek sa samo 1 ciglom
							if (!dvijeOdjednom)
							{
								//provjeravanje s koje strane lopta dolazi
								//trazimo centar lopte te usporedujemo
								int ball_center_X = mBall.Left + (int)(mBall.Width / 2);
								int ball_center_Y = mBall.Top + (int)(mBall.Height / 2);

								if ((ball_center_X >= x.Left && ball_center_X <= x.Right && mBall.Top <= x.Bottom && ballYList[tmpCounter] < 0 ) ||
										(ball_center_X >= x.Left && ball_center_X <= x.Right && mBall.Bottom >= x.Top && ballXList[tmpCounter] > 0))
									//dolazi s gornje ili donje strane
									ballYList[tmpCounter] = -ballYList[tmpCounter];
								else if ((ball_center_Y <= x.Bottom && ball_center_Y >= x.Top && mBall.Right >= x.Left && ballXList[tmpCounter] > 0) ||
										(ball_center_Y <= x.Bottom && ball_center_Y >= x.Top && mBall.Left <= x.Right && ballXList[tmpCounter] < 0))
									//dolazi s lijeva ili desna
									ballXList[tmpCounter] = -ballXList[tmpCounter];
								else if ((ball_center_X < x.Left && ball_center_Y > x.Bottom) ||
											(ball_center_Y > x.Bottom && ball_center_X < x.Left))
								//udara u lijevi donji rub
								{
									ballYList[tmpCounter] = Math.Abs(ballYList[tmpCounter]);
									ballXList[tmpCounter] = -Math.Abs(ballXList[tmpCounter]);
								}
								else if ((ball_center_X > x.Right && ball_center_Y > x.Bottom) ||
											(ball_center_Y > x.Bottom && ball_center_X > x.Right))
								//udara u desni donji rub
								{
									ballYList[tmpCounter] = Math.Abs(ballYList[tmpCounter]);
									ballXList[tmpCounter] = Math.Abs(ballXList[tmpCounter]); 
								}
								else if ((ball_center_X < x.Left && ball_center_Y < x.Top) ||
											(ball_center_Y < x.Top && ball_center_X < x.Left))
								//udara u gornji lijevi rub
								{
									ballYList[tmpCounter] = -Math.Abs(ballYList[tmpCounter]);
									ballXList[tmpCounter] = -Math.Abs(ballXList[tmpCounter]);
								}
								else if ((ball_center_X > x.Right && ball_center_Y < x.Top) ||
											(ball_center_Y < x.Top && ball_center_X > x.Right))
								//udara u gornji desni rub									
								{
									ballYList[tmpCounter] = -Math.Abs(ballYList[tmpCounter]);
									ballXList[tmpCounter] = Math.Abs(ballXList[tmpCounter]);
								}
								//funkcija koja unistava ciglu ili staticki efekt x
								destroyBlock(x);
							}
							// 2. slučaj dodirujemo dvije cigle odjednom
							else if (dvijeOdjednom)
							{
								//trazimo centar lopte te usporedujemo
								int ball_center_X = mBall.Left + (int)(mBall.Width / 2);
								int ball_center_Y = mBall.Top + (int)(mBall.Height / 2);
								int x_center_X = x.Left + (int)(x.Width / 2);
								int x_center_Y = x.Top + (int)(x.Height / 2);
								int y_center_X = y.Left + (int)(y.Width / 2);
								int y_center_Y = y.Top + (int)(y.Height / 2);

								if ((ball_center_X >= x.Left && ball_center_X <= y.Right) ||
											(ball_center_X >= y.Left && ball_center_X <= x.Right))
								//dolazi s gornje ili donje strane
								{
									ballYList[tmpCounter] = -ballYList[tmpCounter];

									//spriječimo uništavanje druge u sljedećoj iteraciji igrice
									if (Math.Abs(mBall.Bottom - x.Top) > Math.Abs(mBall.Top - x.Bottom))
										mBall.Top = x.Bottom + 1;
									else
										mBall.Top = x.Top - 1 - mBall.Height;

									//uništavamo bližu ciglu
									if (Math.Abs(ball_center_X - x_center_X) > Math.Abs(ball_center_X - y_center_X))
										destroyBlock(y);
									else
										destroyBlock(x);
								}
								else
								//dolazi s lijeva ili desna
								{
									ballXList[tmpCounter] = -ballXList[tmpCounter];

									//spriječimo uništavanje druge u sljedećoj iteraciji igrice
									if (Math.Abs(mBall.Left - x.Right) > Math.Abs(mBall.Right - x.Left))
										mBall.Left = x.Left - 1 - mBall.Width;
									else
										mBall.Left = x.Right + 1;

									//uništavamo bližu ciglu
									if (Math.Abs(ball_center_X - x_center_Y) > Math.Abs(ball_center_X - y_center_Y))
										destroyBlock(y);
									else
										destroyBlock(x);
								}
							}
						}

					}
				}
			}
		}

		private void provjeriUdaranjeOdRub()
        {
			// Za svaku lopticu provjeri je li se sudarila sa zidom
			for(int tmpCounter = 0; tmpCounter < ballList.Count; ++tmpCounter)
            {
				PictureBox mBall = ballList[tmpCounter];

				//Ako je lopta išla prema lijevo i udarila u lijevi rub, odbija se(isto za gore i desno),
				if ((mBall.Left < 0 && ballXList[tmpCounter] < 0) || (mBall.Right > splitContainer1.Panel2.Width && ballXList[tmpCounter] > 0))
				{
					ballXList[tmpCounter] = -ballXList[tmpCounter];
					playSound("wall");
				}
				if (mBall.Top < 0 && ballYList[tmpCounter] < 0)
				{
					ballYList[tmpCounter] = -ballYList[tmpCounter];
					playSound("wall");
				}
			}
		}

		private void provjeriUdaranjeOdIgraca()
        {

			for (int tmpCounter = 0; tmpCounter < ballList.Count; ++tmpCounter)
			{
				PictureBox mBall = ballList[tmpCounter];

				//kut pod kojim se loptica odbija je zadan time gdje je udrila o plocu
				if (mBall.Bounds.IntersectsWith(player.Bounds))
				{
					//isti zvuk kao i kad udara u zid
					playSound("wall");

					//pozicija gdje loptica udara o plocu
					double pos = mBall.Width / 2 + mBall.Left;

					double sredina_ploce = player.Left + player.Width / 2;
					double omjer = 2 * (pos - sredina_ploce) / player.Width;

					//korigiranje rubnih slučajeva
					omjer = (omjer < -1) ? -1 : omjer;
					omjer = (omjer > 1) ? 1 : omjer;
					//mapiranje intervala [-1,1]->[PI,0]
					//koliko će mjesto sudaranja lopte i ploče utjecati na
					//kut odbijanja lopte
					double kut = Math.PI / 2 + omjer * (-Math.PI / 2);


					//nedaj kut manji od PI/7 ili veći od 6PI/7
					kut = Math.Abs(kut) < Math.PI / 7 ? Math.PI / 7 : kut;
					kut = Math.Abs(kut) > 6 * Math.PI / 7 ? 6 * Math.PI / 7 : kut;

					ballYList[tmpCounter] = -Math.Sin(kut) * ball_speed;
					ballXList[tmpCounter] = Math.Cos(kut) * ball_speed;

				}
            }
		}

		// gleda micemo li plocu 
		private void keyisdown(object sender, KeyEventArgs e)
		{
			if(e.KeyCode == Keys.Left)
			{
				goLeft = true;
			}
			else if (e.KeyCode == Keys.Right)
			{
				goRight = true;
			}
		}

		// Prestanak kretanja igraca
		private void keyisup(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Left)
			{
				goLeft = false;
			}
			if (e.KeyCode == Keys.Right)
			{
				goRight = false;
			}
			// Kad smo zavrsili, klikom na enter pokrecemo novu igru
			if (e.KeyCode == Keys.Enter && isGameOver == true)
			{
				removeBlocks();
				placeBlocks();
			}
		}
		//broji vrijeme u sekundama, pocinje kad se ispali loptica
        private void timer1_Tick(object sender, EventArgs e)
        {
			counter++;
			int seconds = counter % 60;
			int minutes = counter / 60;
			label2.Text = minutes.ToString("D2") + ":" + seconds.ToString("D2");

			time_to_shift++;

			mSeconds++;
			//Dodajemo ubrzanje loptice nakon sto porde odredeni period vremena.
			//Ubrzanje se dogada svakih 30 sekundi
			if(mSeconds > 30)
            {
				mSeconds = 0;
				standard_ball_speed += 1;
				if (ball_speed == standard_ball_speed - 1)
                {
					ball_speed = standard_ball_speed;
					adjustBallSpeed();
                }
            }	

			// Ako je time_to_shift > 0 znaci da je pokupljen efekt za brzu ili sporu loptu
			if (fast_slow_time != 0)
				fast_slow_time++;
			if (fast_slow_time > 10)
			{   //ako je proslo 10 sekundi ugasi ga
				fast_slow_time = 0;
				ball_speed = standard_ball_speed;
				//Ako je prethodno doslo do standardnog povecanja brzine,
				// sad ce se namjestiti
				adjustBallSpeed();
			}
		}

		//unistava ciglu x te kreira padajuci efekt ako je potrebno
		private void destroyBlock(Control x)
        {
			if (x.Tag is Block)
			{
				playSound("brick");
				Block blockTag = (Block)x.Tag;
				if (blockTag.blockColor == "darkGreen")
				{
					//Cigla je napukla.
					x.BackgroundImage = Properties.Resources.brokenDarkGreenBrick;
					x.Tag = new Block { blockColor = "brokenDarkGreen" };
				}
				else
				{
					//Razbili smo ciglu.
					if (blockTag.blockColor == "brokenDarkGreen")
					{
						//Promijeniti po zelji. Drugi udarac u ciglu, pa malo veca nagrada.
						score += 50;
					}
					else
						score += 10;

					//Ukloni iz kontrole
					this.splitContainer1.Panel2.Controls.Remove(x);
					//Brisanje iz liste cigli i azuiranje lowest
					lowest = 0;
					foreach (PictureBox p in blockList.ToList())
						if (p.Top == x.Top && p.Left == x.Left)
							blockList.Remove(p);
                        else if (p.Top > lowest) 
							lowest = p.Top;
                        

					//Ako smo pogidili ciglu koja stvara padajuci efekt, ovdje ga stvaramo.
					//Od njega se loptica ne odbija vec samo prolazi preko njega. Cilj ga je 
					//skupiti igracom plocom.
					if (blockTag.blockColor == "purple")
					{
						//Sirina bloka.
						int width = (int)(splitContainer1.Panel2.Width - 14) / 10;
						//stvori blok i postavi mu svojstva
						var effect = new PictureBox();

						effect.Height = 32;
						effect.Width = width;
						effect.BackColor = Color.White;
						//Stvaramo padajuci efekt na mjestu razbijene cigle x.
						effect.Left = x.Left;
						effect.Top = x.Top;


						double effect_decision = rnd.NextDouble();
						if (effect_decision <= 0.3)
						{
							//Stvaramo efekt +50.
							effect.BackgroundImage = Properties.Resources.bonus50;
							effect.Tag = new Effect { Mobile = true, Description = "bonus50" };
						}
						else if (effect_decision <= 0.5)
						{
							//Stvaramo efekt +100.
							effect.BackgroundImage = Properties.Resources.bonus100;
							effect.Tag = new Effect { Mobile = true, Description = "bonus100" };
						}
						else if (effect_decision <= 0.75)
						{
							//Stvaramo efekt Slow - usporavanje loptice za neki (odrediti) koeficijent.

							effect.BackgroundImage = Properties.Resources.slow;
							effect.Tag = new Effect { Mobile = true, Description = "slow" };
						}
						else
						{
							//Stvaramo efekt Fast - ubrzanje loptice za neki (odrediti) koeficijent.
							effect.BackgroundImage = Properties.Resources.fast;
							effect.Tag = new Effect { Mobile = true, Description = "fast" };
						}

						effect.BackgroundImageLayout = ImageLayout.Stretch;
						effectList.Add(effect);
						this.splitContainer1.Panel2.Controls.Add(effect);

					}
				}
			}
			else if (x.Tag is Effect)
            {
				//Ovdje dolazi imeplementacija efekata i njihovo unistavanje.
				//Padajuce efekte dodir s lopticom ne smeta, samo prolazi kroz nju. Oni se skupljaju
				//pomocu igrace ploce.
				//Staticke efekte skupljamo kad ih loptica pogodi. 
				Effect effectTag = (Effect)x.Tag;
				if (!effectTag.Mobile)
				{
					if (effectTag.Description == "destroy")
					{
						playSound("explosion");

						//Ukloni ciglu iz kontrole
						this.splitContainer1.Panel2.Controls.Remove(x);

						//brisanje iz liste efekata
						lowest = 0;
						foreach (PictureBox p in effectList.ToList())
							if (p.Top == x.Top && p.Left == x.Left)
								effectList.Remove(p);
							else if (p.Top > lowest)
							{
								Effect ef = (Effect)p.Tag;
								if (!ef.Mobile)
									lowest = p.Top;
							}

						destroySurroundingBlocks(x); //unistava okolne cigle
					}
					else if (effectTag.Description == "newBall")
					{
						//Efekt stvaranja novih loptica.
						this.splitContainer1.Panel2.Controls.Remove(x);
						lowest = 0;
						foreach (PictureBox p in effectList.ToList())
							if (p.Top == x.Top && p.Left == x.Left)
								effectList.Remove(p);
							else if (p.Top > lowest)
                            {
								//azuriraj lowest
								Effect ef = (Effect)p.Tag;
								if (!ef.Mobile)
									lowest = p.Top;
							} 
								
						createNewBalls(x);
					}
				}
			}
		}

		//Stvara nove loptice na mjestu cigle x
		private void createNewBalls(Control x)
        {
			for(int i = 0; i < 2; ++i)
            {
				var newBall = new PictureBox();
				newBall.Height = 26;
				newBall.Width = 26;
				newBall.BackColor = SystemColors.ControlDarkDark;
				newBall.Left = x.Left + x.Width / 2 - newBall.Width / 2;
				newBall.Top = x.Top - newBall.Height;
				newBall.BackgroundImage = Properties.Resources.circle_cropped;
				newBall.BackgroundImageLayout = ImageLayout.Stretch;
				ballList.Add(newBall);
				this.splitContainer1.Panel2.Controls.Add(newBall);

				//Kod stvaranja novih loptica racunamo ballX i ballY vrijednosti pod kojim krecu.
				ballXList.Add(Math.Cos(0.5 * Math.PI + 0.05 * i * Math.PI) * ball_speed);
				ballYList.Add(-Math.Sin(0.5 * Math.PI + 0.05 * i * Math.PI) * ball_speed);

			}

		}

		//Funkcija uzima ciglu x i unistava cigle koje je okruzuju
		private void destroySurroundingBlocks(Control x)
		{
			
			var rect = new Rectangle(x.Left - 5, x.Top - 5 ,x.Width + 10, x.Height + 10);
			foreach (Control c in this.splitContainer1.Panel2.Controls)
			{
				if (c is PictureBox && (c.Tag is Block || c.Tag is Effect))
				{
					var c_rect = new Rectangle(c.Left, c.Top, c.Width, c.Height);
					if (c_rect.IntersectsWith(rect))
						destroyBlock(c);

				}
			}
		}

        // Funkcija namjesta ballX i ballY za sve lopte, koristi se kad se promjeni ball_speed,
        void adjustBallSpeed()
        {
			for (int i = 0; i < ballXList.Count(); i++)
			{
				double kut = Math.Atan2(ballYList[i], ballXList[i]);
				ballXList[i] = ball_speed * Math.Cos(kut);
				ballYList[i] = ball_speed * Math.Sin(kut);
			}	
		}

		private void splitContainer1_Panel2_MouseDown(object sender, MouseEventArgs e)
        {
			if (ball_speed == 0)
            {
				//pokretanje igre
				//prvo pronadi gdje treba usmjeriti lopticu
				//ako je klik bio prenisko, zanemari
				if (e.Y > player.Top - 5)
					return;
				
				int sredina_lopte_X = ballList[0].Left + (int)ballList[0].Width / 2;
				int sredina_lopte_Y = ballList[0].Top;
				int a = Math.Abs((e.X - splitContainer1.Panel1.Width) - sredina_lopte_X);
				int b = Math.Abs(e.Y - sredina_lopte_Y);
			
				double kut = Math.Atan2(b,a);
				
				//ako je klik bio s lijeve strane translatiraj ga
				if ((e.X - splitContainer1.Panel1.Width) < sredina_lopte_X)
					kut = Math.PI - kut;

				ball_speed = 13;
				standard_ball_speed = ball_speed;
				textBox1.Text = "";

				//ball_speed moze biti 0 samo kad imamo jednu lopticu, tj prije pocetka igre
				ballXList[0] = Math.Cos(kut) * ball_speed;
				ballYList[0] = -Math.Sin(kut) * ball_speed;

				//zapocni timer u za igru u sekundama
				playSound("start");
				timer1.Start();
			}
        }

    }
}
