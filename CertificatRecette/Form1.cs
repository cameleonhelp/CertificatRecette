using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace CertificatRecette
{
    public partial class Form1 : Form
    {
        private Thread t;
        public bool bReadyDist = false;
        public bool bReadySNCF = false;
        public bool bReadyRootPath = false;
        public bool bReadyMasterName = false;
        public string HKCUValidRecette = "HKEY_CURRENT_USER\\SOFTWARE\\e.SNCF\\CertificatRecette\\Settings";
        public enum logtype { Information, Success, Warning, Error, Failure };
        public static string logfile = @"C:\e.SNCF\logs\CertificatRecette.log";
        public static string txtResult = Path.Combine(Application.StartupPath,"CerticatResult.txt");
        public string listDistributeurs = "";
        public bool changefinish = true;
        public string pathSNCF = "";
        public string pathDIST = "";
        public string lastPathSNCF = "";
        public string lastPathDIST = "";
        public string pathResult = "";
        public string fileDiffer = "";
        public bool isFailed = false;

        public Form1()
        {
            t = new Thread(new ThreadStart(StartSplashScreen));
            //t.Start();
            InitializeComponent();
            //Thread.Sleep(10000);
            //t.Abort();
        }

        public void StartSplashScreen()
        {
            Application.Run(new SplashForm());
        }

        private void Log(int iLogType, string logMessage, bool result)
        {
            Thread.Sleep(50);
            using (StreamWriter w = File.AppendText(logfile))
            {
                w.WriteLine(DateTime.Now.ToString("") + ";" + Enum.GetName(typeof(logtype), iLogType) + ";" + logMessage);
            }
            if (result)
            {
                if (cbSaveInFolderSNCF.Checked)
                {
                    if (cbSNCFR7.Text != "")
                    {
                        using (StreamWriter w = File.AppendText(txtResult))
                        {
                            w.WriteLine(logMessage);
                        }
                    }
                }
                else
                {
                    using (StreamWriter w = File.AppendText(txtResult))
                    {
                        w.WriteLine(logMessage);
                    }
                }
            }
        }

        private string GetOsSelected()
        {
            if (rbWin10.Checked)
            {
                return "10";
            } else
            {
                return "7";
            }
        }

        private string LastFolderFor(string rootpath)
        {
            try
            {
                string biggerSubDir = "";
                int bigger = 0;
                if (GetOsSelected() == "10")
                {
                    string[] subDirs = Directory.GetDirectories(rootpath, "* 15.*");
                    Array.Sort<string>(subDirs, new Comparison<string>((i1, i2) => i2.CompareTo(i1)));
                    foreach (string subdir in subDirs)
                    {
                        DirectoryInfo subDirInfo = new DirectoryInfo(subdir);
                        if (subDirInfo.Name.Contains(" 15.") && txtNewFolder.Text != subDirInfo.Name)
                        {
                            string extractversion = Regex.Match(subDirInfo.Name, @"\d+(?:\.\d+(?:\.\d+))?").Value;
                            int version = FormatVersion(extractversion);
                            if (version > bigger)
                            {
                                if (Directory.GetFiles(subDirInfo.FullName, "*.doc?").Count() > 0)
                                {
                                    bigger = version;
                                    biggerSubDir = subDirInfo.Name;
                                }
                            }
                        }
                    }
                    if (biggerSubDir != "")
                    {
                        return Path.Combine(rootpath, biggerSubDir);
                    }
                    else
                    {
                        return rootpath;
                    }
                }
                else
                {
                    string[] subDirs = Directory.GetDirectories(rootpath,"* 11.*");
                    Array.Sort<string>(subDirs, new Comparison<string>((i1, i2) => i2.CompareTo(i1)));
                    foreach (string subdir in subDirs)
                    {
                        DirectoryInfo subDirInfo = new DirectoryInfo(subdir);
                        if (subDirInfo.Name.Contains(" 11.") && txtNewFolder.Text != subDirInfo.Name)
                        {
                            string extractversion = Regex.Match(subDirInfo.Name, @"\d+(?:\.\d+(?:\.\d+))?").Value;
                            int version = FormatVersion(extractversion);
                            if (version > bigger)
                            {
                                if (Directory.GetFiles(subDirInfo.FullName, "*.doc?").Count() > 0)
                                {
                                    bigger = version;
                                    biggerSubDir = subDirInfo.Name;
                                }
                            }
                        }
                    }
                    if (biggerSubDir != "")
                    {
                        return Path.Combine(rootpath, biggerSubDir);
                    }
                    else
                    {
                        return rootpath;
                    }
                }
            }
            catch (Exception ex)
            {
                return "";
                throw new Exception(ex.Message);
            }
        }

        private int FormatVersion(string version)
        {
            try
            {
                string[] aDigit = version.Split('.');
                string result = "";
                foreach (string digit in aDigit)
                {
                    string value = digit;
                    if (Convert.ToInt32(value) < 10 && value.Length < 2)
                    {
                        value = "0" + value;
                    }
                    result += value;
                }
                if (result != "")
                {
                    return Convert.ToInt32(result);
                } else
                {
                    return Convert.ToInt32(version.Replace(".", ""));
                }
            } catch (Exception ex) {
                return Convert.ToInt32(version.Replace(".", ""));
                throw new Exception(ex.Message);
            }
        }

        private void BindCbMasters()
        {
            try
            {
                Log(0, "Chargement de la liste des masters.", false);
                string[] subDirs = Directory.GetDirectories(txtRacineFolder.Text, "Master logique version *");
                cbMasters.Items.Clear();
                cbMasters.Items.Add("");
                foreach (string subdir in subDirs)
                {
                    DirectoryInfo subDirInfo = new DirectoryInfo(subdir);
                    if (subDirInfo.Name.Contains("Master logique version "))
                    {
                        Log(0, "Ajout de : " + subDirInfo.Name, false);
                        cbMasters.Items.Add(subDirInfo.Name);
                    }
                }
                pathSNCF = Path.Combine(txtRacineFolder.Text, cbMasters.Text + @"\Recettes\SNCF");
                if (Directory.Exists(Path.Combine(txtRacineFolder.Text, cbMasters.Text + @"\Recette\SNCF")))
                {
                    pathSNCF = Path.Combine(txtRacineFolder.Text, cbMasters.Text + @"\Recette\SNCF");
                }
            }
            catch (Exception ex)
            {
                Log(3, "Erreur lors du chargement de la liste des masters. " + ex.Message + " A partir de : " + txtRacineFolder.Text, false);
                ex.GetType();
            }
        }

        private void BindCBSNCFR7()
        {
            try
            {
                ToolTip toolTip1 = new ToolTip();
                pbAlertSNCF.Visible = false;
                Log(0, "Chargement de la liste des recettes SNCF.", false);
                cbSNCFR7.Text = "";
                cbSNCFR7.Items.Clear();
                cbSNCFR7.Items.Add("");
                string tooltipMessage = "";
                if (pathSNCF != "" && cbMasters.Text != "")
                {
                    string[] subDirs = Directory.GetDirectories(pathSNCF);
                    foreach (string subdir in subDirs)
                    {
                        DirectoryInfo subDirInfo = new DirectoryInfo(subdir);
                        if (File.Exists(Path.Combine(subDirInfo.FullName, "Version_Master.txt")))
                        {
                            Log(0, "Ajout de : " + subDirInfo.Name, false);
                            cbSNCFR7.Items.Add(subDirInfo.Name);
                        }
                        else
                        {
                            tooltipMessage += subDirInfo.Name + "\r\n";
                        }
                    }
                    if (tooltipMessage != "")
                    {
                        pbAlertSNCF.Visible = true;
                        toolTip1.SetToolTip(pbAlertSNCF, "Il existe d'autres dossiers non visibles :\r\n" + tooltipMessage);
                    }
                }
                if (cbSNCFR7.Text != "")
                    pathSNCF = Path.Combine(pathSNCF, cbSNCFR7.Text);
            }
            catch (Exception ex)
            {
                Log(3, "Erreur lors du chargement de la liste des recettes SNCF. " + ex.Message + " A partir de : " + pathSNCF, false);
                ex.GetType();
            }
        }

        private void AdjustWidthComboBox_DropDown(object sender, System.EventArgs e)
        {
            ComboBox senderComboBox = (ComboBox)sender;
            int width = senderComboBox.DropDownWidth;
            Graphics g = senderComboBox.CreateGraphics();
            Font font = senderComboBox.Font;
            int vertScrollBarWidth =
                (senderComboBox.Items.Count > senderComboBox.MaxDropDownItems)
                ? SystemInformation.VerticalScrollBarWidth : 0;

            int newWidth;
            foreach (string s in ((ComboBox)sender).Items)
            {
                newWidth = (int)g.MeasureString(s, font).Width
                    + vertScrollBarWidth;
                if (width < newWidth)
                {
                    width = newWidth;
                }
            }
            senderComboBox.DropDownWidth = width + 5;
        }

        private void BindCbDistributeurs()
        {
            string distributeurs = (string)Registry.GetValue(HKCUValidRecette, "ListDistributeur", "");
            Log(0, "Chargement de la liste des distributeurs", false);
            Array arListDist = distributeurs.Split(';');
            cbDistributeurs.Items.Clear();
            cbDistributeurs.Items.Add("");
            foreach (string value in arListDist)
            {
                if (value != "")
                    cbDistributeurs.Items.Add(value);
            }
        }

        private void BindCBDISTR7()
        {
            try
            {
                ToolTip toolTip = new ToolTip();
                pbAlertDistributeur.Visible = false;
                Log(0, "Chargement de la liste des recettes Distributeur.", false);
                cbDISTR7.Text = "";
                cbDISTR7.Items.Clear();
                cbDISTR7.Items.Add("");
                string tooltipMessage1 = "";
                if (pathDIST != "" && cbDistributeurs.Text != "")
                {
                    string[] subDirs = Directory.GetDirectories(pathDIST);
                    foreach (string subdir in subDirs)
                    {
                        DirectoryInfo subDirInfo = new DirectoryInfo(subdir);
                        if (File.Exists(Path.Combine(subDirInfo.FullName, "Version_Master.txt")))
                        {
                            Log(0, "Ajout de : " + subDirInfo.Name, false);
                            cbDISTR7.Items.Add(subDirInfo.Name);
                        } else
                        {
                            tooltipMessage1 += subDirInfo.Name + "\r\n";
                        }
                    }
                    if (tooltipMessage1 != "")
                    {
                        pbAlertDistributeur.Visible = true;
                        toolTip.SetToolTip(pbAlertDistributeur, "Il existe d'autre(s) dossier(s) non visible(s) :\r\n" + tooltipMessage1);
                    }
                }
                if (cbDISTR7.Text != "")
                    pathDIST = Path.Combine(pathDIST, cbDISTR7.Text);
            }
            catch (Exception ex)
            {
                Log(3, "Erreur lors du chargement de la liste des recettes Distributeur. " + ex.Message + " A partir de : " + pathDIST, false);
                ex.GetType();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            t.Start();
            //Initialisation du fichier log
            if (File.Exists(logfile))
            {
                File.Delete(logfile);
            }
            if (!Directory.Exists(@"C:\e.SNCF\logs"))
            {
                Directory.CreateDirectory(@"C:\e.SNCF\logs");
            }
            Log(0, "Version " + Application.ProductVersion + " - " + FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location).LegalCopyright,true);
            Log(0, "--- DEBUT INITIALISATION DU PROGRAMME ---",false);
            var productInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
            SSVersion.Text = Application.ProductVersion + " " + productInfo.LegalCopyright;
            txtRacineFolder.Text = (string)Registry.GetValue(HKCUValidRecette, "RootPath", @"\\commun\dsit_buro\Communs\DSIT\TSP\PCIR\GCP\Materiel_activites\MasterSNCF\");
            BindCbMasters();
            BindCbDistributeurs();
            //si exist dans la base de registre, on charge dans le cas contraire on mets la valeur par défaut
            Log(0, "Récupérartion des informations en base de registre",false);
            txtNewFolder.Text = (string)Registry.GetValue(HKCUValidRecette, "MasterName", "Master logique version XX.XX.XX");
            cbMasters.Text = (string)Registry.GetValue(HKCUValidRecette, "lastMaster", "");
            cbDistributeurs.Text = (string)Registry.GetValue(HKCUValidRecette, "LastDistributeur", "");
            listDistributeurs = (string)Registry.GetValue(HKCUValidRecette, "ListDistributeur", "");

            //enregistrement en base de registre
            Log(0, "Sauvegarde des informations en base de registre",false);
            Registry.SetValue(HKCUValidRecette, "Version", Application.ProductVersion, RegistryValueKind.String);
            Registry.SetValue(HKCUValidRecette, "RootPath", txtRacineFolder.Text, RegistryValueKind.String);
            Registry.SetValue(HKCUValidRecette, "MasterName", txtNewFolder.Text, RegistryValueKind.String);
            Registry.SetValue(HKCUValidRecette, "lastMaster", cbMasters.Text, RegistryValueKind.String);
            Registry.SetValue(HKCUValidRecette, "LastDistributeur", cbDistributeurs.Text, RegistryValueKind.String);
            //listDistributeurs = "";

            foreach (string dist in cblDist.Items)
            {
                listDistributeurs += dist + ";";
            }
            Registry.SetValue(HKCUValidRecette, "ListDistributeur", listDistributeurs.TrimEnd(';'), RegistryValueKind.String);

            Log(0, "Chargement de la liste des distributeurs", false);
            Array arListDist = listDistributeurs.Split(';');
            foreach(string value in arListDist)
            {
                if (value != "")
                    cblDist.Items.Add(value);
            }
            if (txtRacineFolder.Text != "" && Directory.Exists(txtRacineFolder.Text))
            {
                string rootSubFolder = LastFolderFor(txtRacineFolder.Text);
                try
                {
                    string[] aSubFolderFiles = Directory.GetFiles(rootSubFolder, "*", SearchOption.TopDirectoryOnly);

                    foreach (string filename in aSubFolderFiles)
                    {
                        FileInfo fi = new FileInfo(filename);
                        txtBiggerDirFullName.Text = fi.DirectoryName;
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }

            Log(0, "Initailisation du choix du système d'exploitation", false);
            SelectCBOS();

            Log(0, "Initialisation du bouton pour l'arborescncee", false);
            //Initialisation du bouton de generation de l'arborescence
            if (txtRacineFolder.Text == "")
            {
                bReadyRootPath = false;
            }
            else
            {
                bReadyRootPath = true;
            }
            if (txtNewFolder.Text == "")
            {
                bReadyMasterName = false;
            }
            else
            {
                bReadyMasterName = true;
            }
            if (bReadyMasterName && bReadyRootPath)
            {
                bGeneratePath.Enabled = true;
            } else if (!bReadyRootPath)
            {
                bGeneratePath.Enabled = false;
            }

            BindCBSNCFR7();
            BindCBDISTR7();

            pathDIST = Path.Combine(txtRacineFolder.Text, cbMasters.Text + @"\Recettes\" + cbDistributeurs.Text);
            if (Directory.Exists(Path.Combine(txtRacineFolder.Text, cbMasters.Text + @"\Recette\" + cbDistributeurs.Text)))
            {
                pathDIST = Path.Combine(txtRacineFolder.Text, cbMasters.Text + @"\Recette\" + cbDistributeurs.Text);
            }
            pathSNCF = Path.Combine(txtRacineFolder.Text, cbMasters.Text + @"\Recettes\SNCF");
            if (Directory.Exists(Path.Combine(txtRacineFolder.Text, cbMasters.Text + @"\Recette\SNCF")))
            {
                pathSNCF = Path.Combine(txtRacineFolder.Text, cbMasters.Text + @"\Recette\SNCF");
            }

            Log(0, "--- FIN INITIALISATION DU PROGRAMME ---",false);
            t.Abort();
        }

        private void SelectCBOS()
        {
            rbWin10.Checked = true;
            rbWin7.Checked = false;
            if (txtNewFolder.Text.Contains(" 11."))
            {
                rbWin10.Checked = false;
                rbWin7.Checked = true;
            }
        }

        private void pictureBox6_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            panel1.Visible = !panel1.Visible;
            panel1.BringToFront();
        }

        private void pictureBox7_Click(object sender, EventArgs e)
        {
            panel2.Visible = !panel2.Visible;
            panel2.BringToFront();
        }

        private void pictureBox8_Click(object sender, EventArgs e)
        {
            panel3.Visible = !panel3.Visible;
            panel3.BringToFront();
        }

        private void panel4_Paint(object sender, PaintEventArgs e)
        {
        }

        private void pictureBox10_Click(object sender, EventArgs e)
        {
            panel5.Visible = !panel5.Visible;
            panel5.BringToFront();
        }

        private void pictureBox11_Click(object sender, EventArgs e)
        {
            panel6.Visible = !panel6.Visible;
            panel6.BringToFront();
        }

        private void pictureBox9_Click(object sender, EventArgs e)
        {
            panel4.Visible = !panel4.Visible;
            panel4.BringToFront();

        }

        private void pictureBox5_Click(object sender, EventArgs e)
        {

        }

        private void txtRacineFolder_TextChanged(object sender, EventArgs e)
        {
            if (txtRacineFolder.Text == "")
            {
                bReadyRootPath = false;
            }

            if (bReadyMasterName && bReadyRootPath)
            {
                bGeneratePath.Enabled = true;
            }
            else if (!bReadyRootPath)
            {
                bGeneratePath.Enabled = false;
            }
            Registry.SetValue(HKCUValidRecette, "RootPath", txtRacineFolder.Text, RegistryValueKind.String);
        }

        private void openExplorer(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                try
                {
                    ProcessStartInfo l_psi = new ProcessStartInfo();
                    l_psi.FileName = "explorer";
                    l_psi.Arguments = string.Format("/root,{0}", folderPath);
                    l_psi.UseShellExecute = true;

                    Process l_newProcess = new Process();
                    l_newProcess.StartInfo = l_psi;
                    Log(0, "Ouverture de l'explorateur Windows dans le dossier : " + folderPath,false);
                    l_newProcess.Start();
                }
                catch (Exception ex)
                {
                    Log(3, "Echec d'ouverture de l'explorateur Windows dans le dossier : " + folderPath,false);
                    throw new Exception("Impossible d'ouvrir l'explorateur WIndows.", ex);
                }
            }
            else
            {
                Log(3, "Dossier : " + folderPath + " est introuvable",true);
                MessageBox.Show("Le dossier " + folderPath + " est introuvable.", "Dossier non trouvé", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void openFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    Log(0, "Ouverture du fichier :" + filePath,false);
                    Process.Start(filePath);
                } catch(Exception ex)
                {
                    Log(3, "Echec d'ouverture du fichier :" + filePath,false);
                    throw new Exception("Impossible d'ouvrir le fichier avec son application par défaut.", ex);
                }
            } else
            {
                Log(3, "Fichier :" + filePath + " est introuvable",true);
                MessageBox.Show("Le fichier " + filePath + " est introuvable.", "Fichier non trouvé", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void txtNewFolder_TextChanged(object sender, EventArgs e)
        {
            if (txtNewFolder.Text == "")
            {
                bReadyMasterName = false;
                txtNewFolder.Text = "Master logique version XX.XX.XX";
            }
            else
            {
                bReadyMasterName = true;
            }
            if (bReadyMasterName && bReadyRootPath)
            {
                bGeneratePath.Enabled = true;
            }
            else if (!bReadyRootPath)
            {
                bGeneratePath.Enabled = false;
            }
            Registry.SetValue(HKCUValidRecette, "MasterName", txtNewFolder.Text, RegistryValueKind.String);
            SelectCBOS();
        }

        private void button2_Click(object sender, EventArgs e)
        {
        }

        private void bBrowseRootFolder_Click(object sender, EventArgs e)
        {
            if (txtRacineFolder.Text != "") { 
                fbd.SelectedPath = txtRacineFolder.Text;
            }
            else
            {
                fbd.SelectedPath = null;
            }

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txtRacineFolder.Text = fbd.SelectedPath;
            }
        }

        private void bBrowseFolderDist_Click(object sender, EventArgs e)
        {
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Registry.SetValue(HKCUValidRecette, "Version", Application.ProductVersion, RegistryValueKind.String);
            Registry.SetValue(HKCUValidRecette, "RootPath", txtRacineFolder.Text, RegistryValueKind.String);
            Registry.SetValue(HKCUValidRecette, "MasterName", txtNewFolder.Text, RegistryValueKind.String);
            Registry.SetValue(HKCUValidRecette, "PathInterne", cbMasters.Text, RegistryValueKind.String);
            Registry.SetValue(HKCUValidRecette, "lastMaster", cbMasters.Text, RegistryValueKind.String);
            Registry.SetValue(HKCUValidRecette, "LastDistributeur", cbDistributeurs.Text, RegistryValueKind.String);
            listDistributeurs = "";
            foreach (string dist in cblDist.Items)
            {
                listDistributeurs += dist + ";";
            }
            Registry.SetValue(HKCUValidRecette, "ListDistributeur", listDistributeurs.TrimEnd(';'), RegistryValueKind.String);
        }

        private void bR7SNCF1_Click(object sender, EventArgs e)
        {
            openFile(Path.Combine(pathSNCF, "Version_Master.txt"));
        }

        private void bDist1_Click(object sender, EventArgs e)
        {
            openFile(Path.Combine(pathDIST, "Version_Master.txt"));
        }

        private void bR7SNCF2_Click(object sender, EventArgs e)
        {
            openFile(Path.Combine(pathSNCF, @"DeploymentLogs\BDD.log"));
        }

        private void bDist2_Click(object sender, EventArgs e)
        {
            openFile(Path.Combine(pathDIST, @"DeploymentLogs\BDD.log"));
        }

        private void bR7SNCF3_Click(object sender, EventArgs e)
        {
            openFile(Path.Combine(pathSNCF, @"DeploymentLogs\Results.xml"));
        }

        private void bDist3_Click(object sender, EventArgs e)
        {
            openFile(Path.Combine(pathDIST, @"DeploymentLogs\Results.xml"));
        }

        private string FindFileByExtension(string path,string extension)
        {
            string result = null;
            if (Directory.Exists(path))
            {
                string[] fileEntries = Directory.GetFiles(path);
                foreach (string fileName in fileEntries)
                {
                    FileInfo fi = new FileInfo(fileName);
                    if (fi.Extension.ToLower() == "." + extension)
                    {
                        Log(0, "Fichier NFO trouvé : " + fileName,true);
                        result = fileName;
                    }
                }
            }
            else
            {
                MessageBox.Show("Dossier" + path + " introuvable.", "Dossier non trouvé", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Log(2, "Dossier non trouvé : " + path,true);
            }
            if (result == null)
            {
                Log(2, "Fichier " + extension.ToUpper() + " non trouvé dans le dossier: " + path, true);
            }
            return result;
        }

        private string FindFileByNameContains(string path, string partialname)
        {
            string result = null;
            try
            {
                if (Directory.Exists(path))
                {
                    string[] fileEntries = Directory.GetFiles(path);
                    foreach (string fileName in fileEntries)
                    {
                        FileInfo fi = new FileInfo(fileName);
                        if (fi.Name.ToLower().Contains(partialname.ToLower()))
                        {
                            Log(0, "Fichier trouvé : " + fileName, true);
                            result = fileName;
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Dossier" + path + " introuvable.", "Dossier non trouvé", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Log(2, "Dossier non trouvé : " + path, true);
                }
                if (result == null)
                {
                    Log(2, "Fichier contenant " + partialname.ToUpper() + " est non trouvé dans le dossier: " + path, true);
                }
            } catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return result;
        }

        private bool CompareNFOFile(string file1, string file2)
        {
            bool result = false;

            return result;
        }

        public IEnumerable<string> FindProblemInNFO(string file)
        {
            if (file != null)
            {
                return File.ReadLines(file).SkipWhile(line => !line.Contains("[Périphériques à problème]")).Skip(3).TakeWhile(line => !line.Contains("[USB]"));
            } else
            {
                Log(3, "La recherche du fichier n'a pas aboutie",false);
                return null;
            }
        }

        private void bR7SNCF6_Click(object sender, EventArgs e)
        {
            string sFile = FindFileByExtension(pathSNCF, "nfo");
            if (sFile != null)
            {
            Log(0, "Ouverture du fichier " + sFile,false);
                openFile(Path.Combine(pathSNCF, sFile));
            }
            else
            {
                Log(0, "Le fichier NFO est introuvable", true);
                MessageBox.Show("Fichier NFO introuvable dans\r\n" + pathSNCF, "Fichier non trouvé", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void bDist6_Click(object sender, EventArgs e)
        {
            string sFile = FindFileByExtension(pathDIST, "nfo");
            if (sFile != null)
            {
                Log(0, "Ouverture du fichier" + sFile, false);
                openFile(Path.Combine(pathDIST, sFile));
            } else
            {
                Log(0, "Le fichier NFO est introuvable", true);
                MessageBox.Show("Fichier NFO introuvable dans\r\n" + pathDIST, "Fichier non trouvé", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void bR7SNCF4_Click(object sender, EventArgs e)
        {
            openExplorer(Path.Combine(pathSNCF, @"DeploymentLogs"));
        }

        private void bDist4_Click(object sender, EventArgs e)
        {
            openExplorer(Path.Combine(pathDIST, @"DeploymentLogs"));
        }

        private void bR7SNCF5_Click(object sender, EventArgs e)
        {
            openExplorer(pathSNCF);
        }

        private void bDist5_Click(object sender, EventArgs e)
        {
            openExplorer(pathDIST);
        }

        private void bGeneratePath_Click(object sender, EventArgs e)
        {
            Log(0, "--- DEBUT DE LA GENERATION DE L'ARBORESCENCE ---", false);
            try
            {
                string folder = Path.Combine(txtRacineFolder.Text, txtNewFolder.Text);
                if (Directory.Exists(folder))
                {
                    if (MessageBox.Show("Le dossier existe déjà, voulez-vous le supprimer ?\r\nSi NON alors la génération ne sera pas faite.\r\nSi OUI alors le dossier existant sera supprimé.","Dossier existant",MessageBoxButtons.YesNo,MessageBoxIcon.Question) == DialogResult.No)
                    {
                        UnChechCBL(cblDist);
                        UnChechCBL(cblFiles);
                        return;
                    } else
                    {
                        Directory.Delete(folder, true);
                    }
                }
                double pourcent = Math.Round(Convert.ToDouble(60 / cblDist.CheckedItems.Count));
                Cursor.Current = Cursors.WaitCursor;
                pbar.Value = 0;
                string sRoot = Path.Combine(txtRacineFolder.Text, txtNewFolder.Text);
                Log(0, "Création du dossier " + sRoot, false);
                Directory.CreateDirectory(sRoot);
                ProgessBarChange(10);
                string sRecettePath = Path.Combine(sRoot, "Recettes");
                Log(0, "Création du dossier " + sRecettePath, false);
                Directory.CreateDirectory(sRecettePath);
                ProgessBarChange(10);
                foreach (string dist in cblDist.CheckedItems.OfType<string>().ToList())
                {
                    string subDist = Path.Combine(sRecettePath, dist);
                    Log(0, "Création du dossier de recettes " + dist, false);
                    Directory.CreateDirectory(subDist);
                    ProgessBarChange(Convert.ToInt32(pourcent));
                }
                string sSNCF = Path.Combine(sRecettePath, "SNCF");
                Log(0, "Création du dossier de recettes " + sSNCF, false);
                Directory.CreateDirectory(sSNCF);
                ProgessBarChange(10);
                foreach (string file in cblFiles.CheckedItems.OfType<string>().ToList())
                {
                    string source = Path.Combine(txtBiggerDirFullName.Text, file);
                    string destination = Path.Combine(sRoot, file);
                    Log(0, "Copie du fichier " + file, false);
                    File.Copy(source,destination);
                    ProgessBarChange(10);
                }
                pbar.Value = 100;
                //txtRacineFolder.Text = "";
                if (MessageBox.Show("Génération de l'arborescence terminée.\r\nVoulez ouvrir ce dossier ?","Tâche terminée",MessageBoxButtons.YesNo,MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    openExplorer(sRoot);
                }
                pbar.Value = 0;
                UnChechCBL(cblDist);
                UnChechCBL(cblFiles);
            }
            catch (Exception ex)
            {
                Cursor.Current = Cursors.Default;
                Log(3, "Impossible de créer l'arborescence. - " + ex.Message, false);
                ex.GetType();
            }
            Cursor.Current = Cursors.Default;
            Log(0, "--- DEBUT DE LA GENERATION DE L'ARBORESCENCE ---", false);
        }

        private void UnChechCBL(CheckedListBox cbl)
        {
            foreach (int i in cbl.CheckedIndices)
            {
                cbl.SetItemCheckState(i, CheckState.Unchecked);
            }
        }

        private void ProgessBarChange(int step)
        {
            if(pbar.Value < 100)
            {
                pbar.Value += step;
            } else
            {
                pbar.Value = 50;
            }
        }

        private bool CompareFolders(string pathA, string pathB, string except)
        {
            fileDiffer = "";
            bool result = false;
            System.IO.DirectoryInfo dir1 = new System.IO.DirectoryInfo(pathA);
            System.IO.DirectoryInfo dir2 = new System.IO.DirectoryInfo(pathB);
            try
            {
                Log(0, "Début comparaison des dossiers : " + pathA + " et " + pathB, true);
                IEnumerable<FileInfo> list1 = dir1.GetFiles("*.*", SearchOption.AllDirectories);
                IEnumerable<FileInfo> list2 = dir2.GetFiles("*.*", SearchOption.AllDirectories);

                FileCompare myFileCompare = new FileCompare();

                var queryNotCommonFiles1 = list1.Except(list2, myFileCompare);
                var queryNotCommonFiles2 = list2.Except(list1, myFileCompare);

                IEnumerable<FileInfo> listFilter1 = Enumerable.Empty<FileInfo>();
                IEnumerable<FileInfo> listFilter2 = Enumerable.Empty<FileInfo>();


                if (queryNotCommonFiles1.Count() > 0)
                {
                    isFailed = true;
                    Log(0, "", true);
                    Log(0, "Les fichiers suivant sont  dans le dossier SNCF mais pas dans le dossier distributeur : ", true);
                    fileDiffer = "Les fichiers suivant sont  dans le dossier SNCF mais pas dans le dossier distributeur : \r\n";
                    foreach (var v in queryNotCommonFiles1)
                    {
                        if (except == "")
                        {
                            Log(0, v.FullName, true);
                            if (!v.Name.Contains("NFO") && !v.Name.Contains("diskpart") && !v.Name.Contains("zip") && !v.Name.Contains("driverslist") && !v.Name.Contains("export_nfo") && !v.Name.Contains("hotfixes") && !v.Name.Contains("vide") && !v.Name.Contains("systeminfo") && !v.Name.Contains("InfosRecette.log") && !v.Name.Contains("wmic_product"))
                            {
                                fileDiffer += v.FullName + "\r\n";
                                listFilter1 = listFilter1.Append(v);
                            }
                        }
                        else
                        {
                            if (!v.FullName.Contains(except))
                            {
                                Log(0, v.FullName, true);
                                if (!v.Name.Contains("NFO") && !v.Name.Contains("diskpart") && !v.Name.Contains("zip") && !v.Name.Contains("driverslist") && !v.Name.Contains("export_nfo") && !v.Name.Contains("hotfixes") && !v.Name.Contains("vide") && !v.Name.Contains("systeminfo") && !v.Name.Contains("InfosRecette.log") && !v.Name.Contains("wmic_product"))
                                {
                                    fileDiffer += v.FullName + "\r\n";
                                    listFilter1 = listFilter1.Append(v);
                                }
                            }
                        }
                    }
                }
                if (queryNotCommonFiles2.Count() > 0)
                {
                    isFailed = true;
                    Log(0, "", true);
                    Log(0, "Les fichiers suivant sont dans le dossier distributeur mais pas dans le dossier SNCF : ", true);
                    fileDiffer += "\r\n";
                    fileDiffer += "Les fichiers suivant sont dans le dossier distributeur mais pas dans le dossier SNCF : \r\n";
                    foreach (var v in queryNotCommonFiles2)
                    {
                        if (except == "")
                        {
                            Log(0, v.FullName, true);
                            if (!v.Name.Contains("NFO") && !v.Name.Contains("diskpart") && !v.Name.Contains("zip") && !v.Name.Contains("driverslist") && !v.Name.Contains("export_nfo") && !v.Name.Contains("hotfixes") && !v.Name.Contains("vide") && !v.Name.Contains("systeminfo") && !v.Name.Contains("InfosRecette.log") && !v.Name.Contains("wmic_product"))
                            {
                                fileDiffer += v.FullName + "\r\n";
                                listFilter2 = listFilter2.Append(v);
                            }
                        }
                        else
                        {
                            if (!v.FullName.Contains(except))
                            {
                                Log(0, v.FullName, true);
                                if (!v.Name.Contains("NFO") && !v.Name.Contains("diskpart") && !v.Name.Contains("zip") && !v.Name.Contains("driverslist") && !v.Name.Contains("export_nfo") && !v.Name.Contains("hotfixes") && !v.Name.Contains("vide") && !v.Name.Contains("systeminfo") && !v.Name.Contains("InfosRecette.log") && !v.Name.Contains("wmic_product"))
                                {
                                    fileDiffer += v.FullName + "\r\n";
                                    listFilter2 = listFilter2.Append(v);
                                }
                            }
                        }
                    }
                }

                var queryFilterCommonFiles = Enumerable.Empty<FileInfo>();
                var queryCommonFiles = list1.Intersect(list2, myFileCompare);
                if (queryNotCommonFiles1.Count() > queryNotCommonFiles2.Count())
                {
                    queryFilterCommonFiles = listFilter1.Intersect(listFilter2, myFileCompare);
                } else
                {
                    queryFilterCommonFiles = listFilter2.Intersect(listFilter1, myFileCompare);
                }

                if (queryCommonFiles.Count() > 0)
                {
                    Log(0, "", true);
                    Log(0, "Les fichiers suivant sont communs au deux dossiers : ", true);
                    foreach (var v in queryCommonFiles)
                    {
                        Log(0, v.Name, true);
                    }
                }

                if (queryFilterCommonFiles.Count() > 0)
                { 
                    if (listFilter1.Count() == listFilter2.Count())
                    {
                        result = true;
                    }
                    else if (listFilter1.Count() > listFilter2.Count())
                    {
                        Log(3, "Il y a plus de fichiers dans le dossier, " + pathA, true);
                        fileDiffer += "\r\nIl y a plus de fichiers dans le dossier, " + pathA + "\r\n";
                        result = false;
                    }
                    else if (listFilter1.Count() < listFilter2.Count())
                    {
                        Log(3, "Il y a plus de fichiers dans le dossier, " + pathB, true);
                        fileDiffer += "\r\nIl y a plus de fichiers dans le dossier, " + pathB + "\r\n";
                        result = false;
                    }
                    isFailed = true;
                    if (listFilter1.Count() == 0 && listFilter2.Count() == 0)
                    {
                        isFailed = false;
                        Log(0, "Il ne semble pas y avoir d'erreur, à vous de juger...", true);
                        result = true;
                    }
                    else
                    {
                        isFailed = true;
                        Log(3, "Il y a une différence entre les deux dossiers.", true);
                        result = false;
                    }
                }
                else
                {
                    if (listFilter1.Count() == 0 && listFilter2.Count() == 0)
                    {
                        isFailed = false;
                        Log(0, "Il ne semble pas y avoir d'erreur, à vous de juger...", true);
                        result = true;
                    } else
                    {
                        isFailed = true;
                        Log(3, "Il y a une différence entre les deux dossiers.", true);
                        result = false;
                    }
                }
                Log(0, "Le résultat de la comparaison des 2 dossiers est : " + result.ToString(), true);
                Log(0, "", true);
                return result;
            }
            catch (Exception ex)
            {
                Log(3, "Comparaison des dossiers impossible, " + ex.Message, false);
                isFailed = false;
                return result;
                throw new Exception("Compparaison des dossiers impossible.", ex);
            }
        }

        private void initPicture()
        {
            pNeutral1.Visible = true;
            pSucces1.Visible = false;
            pFailed1.Visible = false;
            pbWarning1.Visible = false;

            pNeutral2.Visible = true;
            pSucces2.Visible = false;
            pFailed2.Visible = false;
            pbWarning2.Visible = false;

            pNeutral3.Visible = true;
            pSucces3.Visible = false;
            pFailed3.Visible = false;
            pbWarning3.Visible = false;

            pNeutral4.Visible = true;
            pSucces4.Visible = false;
            pFailed4.Visible = false;
            pbWarning4.Visible = false;

            pNeutral5.Visible = true;
            pSucces5.Visible = false;
            pFailed5.Visible = false;
            pbWarning5.Visible = false;

            pNeutral6.Visible = true;
            pSucces6.Visible = false;
            pFailed6.Visible = false;
            pbWarning6.Visible = false;

            pNeutral7.Visible = true;
            pSucces7.Visible = false;
            pFailed7.Visible = false;
            pbWarning7.Visible = false;
        }

        private string VersionningFile(string fullfilename)
        {
            FileInfo filename = new FileInfo(fullfilename);
            string sfn = filename.Name.Remove(filename.Name.Length - 4);
            string nsfn = sfn;
            string fn = fullfilename;
            string dir = filename.DirectoryName;
            int count = 0;

            while (File.Exists(fn))
            {
                count++;
                nsfn = sfn + "_(version_" + count.ToString() + ")" + filename.Extension;
                fn = Path.Combine(dir,nsfn);
            }
            return fn;
        }

        public bool CompareVersion()
        {
            FileCompare thiscompare = new FileCompare();
            bool result = thiscompare.CompareFile(Path.Combine(pathSNCF, "Version_Master.txt"), Path.Combine(pathDIST, "Version_Master.txt"));
            return result;
        }

        private async void bCompare_Click(object sender, EventArgs e)
        {
            pbar.Value = 0;
            Cursor.Current = Cursors.WaitCursor;
            ToolTip toolTip = new ToolTip();
            if (cbSaveInFolderSNCF.Checked)
            {
                txtResult = VersionningFile(pathResult + "\\" + DateTime.Now.ToString("yyyyMMdd") + "_" + cbSNCFR7.Text + ".txt");
            }
            else
            {
                txtResult = VersionningFile(txtResult);
            }
            //if (File.Exists(txtResult))
            //{

            //    File.Delete(txtResult);
            //}
            if (!Directory.Exists(@"C:\e.SNCF\logs"))
            {
                Directory.CreateDirectory(@"C:\e.SNCF\logs");
            }
            initPicture();

            //Une méthode par comparaison, chaque méthode retourne un boolean
            Log(0, "--- DEBUT DE LA RECETTE AUTOMATIQUE DISTRIBUTEUR ---", false);
            Log(0, "Validation du master " + cbMasters.Text, true);
            Log(0, "A partir de  " + pathSNCF + " et " + pathDIST, true);
            Log(0, "", true);
            Log(0, "Comparaison des fichiers Version_Master.txt", false);
            Cursor.Current = Cursors.WaitCursor;
            FileCompare thiscompare = new FileCompare();
            bool result = await Task.Run(() => { return thiscompare.CompareFile(Path.Combine(pathSNCF, "Version_Master.txt"), Path.Combine(pathDIST, "Version_Master.txt")); });
            //bool result = thiscompare.CompareFile(Path.Combine(pathSNCF, "Version_Master.txt"), Path.Combine(pathDIST, "Version_Master.txt"));
            pbar.Value = 14;
            if (result)
            {
                Log(0, "Resultat de la comparaison des fichiers Version_Master.txt est : " + result.ToString(), true);
                Application.DoEvents();
                pNeutral1.Visible = false;
                pSucces1.Visible = true;
                pSucces1.BringToFront();
                Application.DoEvents();
            }
            else
            {
                Application.DoEvents();
                pNeutral1.Visible = false;
                pFailed1.Visible = true;
                pFailed1.BringToFront();
                string[] SNCFVersionMaster = FileToArray(Path.Combine(pathSNCF, "Version_Master.txt"));
                string[] DistFVersionMaster = FileToArray(Path.Combine(pathDIST, "Version_Master.txt"));
                string[] differElements1 = SNCFVersionMaster.Except(DistFVersionMaster).ToArray();
                string[] differElements2 = DistFVersionMaster.Except(SNCFVersionMaster).ToArray();
                toolTip.SetToolTip(pFailed1, "Attention seul le distributeur doit être différent, dans ce cas c'est normal.");
                Log(3, "Resultat de la comparaison des fichiers Version_Master.txt est : " + result.ToString(), true);
                Log(3, "Attention le distributeur doit être différent, c'est normal.", true);
                Log(3, "Lignes du Version_master.txt différentes dans le fichier SNCF", true);
                string lineDiffer = "Version_master.txt SNCF : \r\n";
                foreach (string line in differElements1)
                {
                    lineDiffer += line + "\r\n";
                    Log(3, line, true);
                }
                Log(3, "Lignes du Version_master.txt différentes dans le fichier DISTRIBUTEUR", true);
                lineDiffer += "\r\nVersion_Master.txt DISTRIBUTEUR : \r\n";
                foreach (string line in differElements2)
                {
                    lineDiffer += line + "\r\n";
                    Log(3, line, true);
                }

                toolTip.SetToolTip(pbWarning1, "Resultat de la comparaison des fichiers Version_Master.txt est : " + result.ToString() + "\r\nAttention seul le distributeur doit être différent, dans ce cas c'est normal.\r\nDans le cas contraire ouvrez les deux fichiers pour voir la différence.\r\n" + lineDiffer);
                pbWarning1.Visible = true;
                Application.DoEvents();
            }
            Log(0, "Comparaison des fichiers BDD.log", false);
            Log(0, "", true);
            Cursor.Current = Cursors.WaitCursor;
            List<string> lstring = await Task.Run(() =>
            {
                return thiscompare.CompareBDDLogFile(Path.Combine(pathSNCF, @"DeploymentLogs\BDD.log"), Path.Combine(pathDIST, @"DeploymentLogs\BDD.log"));
            });
            pbar.Value = 29;
            if (lstring.Count() == 0)
            {
                Log(0, "Resultat de la comparaison des fichiers BDD.log est : " + true.ToString(), true);
                Application.DoEvents();
                pNeutral2.Visible = false;
                pSucces2.Visible = true;
                pSucces2.BringToFront();
                Application.DoEvents();
            }
            else
            {
                Application.DoEvents();
                pNeutral2.Visible = false;
                pFailed2.Visible = true;
                pFailed2.BringToFront();
                Application.DoEvents();
                toolTip.SetToolTip(pFailed2, "Attention certaines informations peuvent être différentes,\r\nil faut mieux ouvrir le fichier BDD.log du distributeur.\r\nFiltrer sur \"FAILURE\" pour n'afficher que les erreurs.");
                Log(3, "Resultat de la comparaison des fichiers BDD.log est : " + false.ToString(), true);
                Log(3, "Attention certaines informations peuvent être différentes, il faut mieux ouvrir le fichier BDD.log du distributeur. Filtrer sur \"FAILURE\" pour n'afficher que les erreurs.", true);
                string tooltipmessage = "";
                foreach (string line in lstring)
                {
                    Log(3, line, true);
                    tooltipmessage += line + "\r\n";
                }
                toolTip.SetToolTip(pbWarning2, "Resultat de la comparaison des fichiers BDD.log est : " + false.ToString() + "\r\nAttention certaines informations peuvent être différentes,\r\nil faut mieux ouvrir le fichier BDD.log du distributeur et le iltrer sur \"FAILURE\"\r\npour n'afficher que les erreurs.\r\n" + tooltipmessage);
                pbWarning2.Visible = true;
            }
            Log(0, "Comparaison des fichiers Results.xml", false);
            Log(0, "", true);
            Cursor.Current = Cursors.WaitCursor;
            result = await Task.Run(() =>
            {
                return thiscompare.CompareFile(Path.Combine(pathSNCF, @"DeploymentLogs\Results.xml"), Path.Combine(pathDIST, @"DeploymentLogs\Results.xml"));
            });
            pbar.Value = 43;
            if (result)
            {
                Log(0, "Resultat de la comparaison des fichiers Results.xml est : " + result.ToString(), true);
                Application.DoEvents();
                pNeutral3.Visible = false;
                pSucces3.Visible = true;
                pSucces3.BringToFront();
                Application.DoEvents();
            }
            else
            {
                Application.DoEvents();
                pNeutral3.Visible = false;
                pFailed3.Visible = true;
                pFailed3.BringToFront();
                Application.DoEvents();
                toolTip.SetToolTip(pFailed3, "Il faut absolument que dans le fichier distributeur,\r\nchaque résulat doir être égal à 0.");
                Log(3, "Resultat de la comparaison des fichiers Results.xml est : " + result.ToString(), true);
                Log(3, "Il faut absolument que dans le fichier distributeur, chaque résulat soit égal à 0.", true);
                toolTip.SetToolTip(pbWarning3, "Resultat de la comparaison des fichiers Results.xml est : " + result.ToString() + "\r\nIl faut absolument que dans le fichier distributeur, chaque résulat soit égal à 0.");
                pbWarning3.Visible = true;
            }
            Log(0, "Comparaison des fichiers export NFO", false);
            Log(0, "", true);
            string fileNFODist = FindFileByNameContains(pathDIST, "export_nfo");
            Cursor.Current = Cursors.WaitCursor;
            IEnumerable<string> list = await Task.Run(() =>
            {
                return FindProblemInNFO(fileNFODist);
            });
            pbar.Value = 57;
            if (list == null || list.Count() < 2)
            {
                result = true;
                Log(0, "Resultat de la comparaison du fichier ...export_nfo.txt du distributeur est : " + result.ToString(), true);
                Application.DoEvents();
                pNeutral6.Visible = false;
                pSucces6.Visible = true;
                pSucces6.BringToFront();
                Application.DoEvents();
            }
            else
            {
                result = false;
                Application.DoEvents();
                pNeutral6.Visible = false;
                pFailed6.Visible = true;
                pFailed6.BringToFront();
                toolTip.SetToolTip(pFailed6, "Il ne doit pas y avoir de périphériques à problème.\r\nConsultez le fichier NFO du distributeur\r\ndans la rubrique \"Composants\\Périphériques à problème\"");
                Application.DoEvents();
                Log(3, "Resultat de la comparaison du fichier ...export_NFO.txt du distributeur est : " + result.ToString(), true);
                Log(3, "Il ne doit pas y avoir de périphériques à problème. Consultez le fichier NFO du distributeur dans la rubrique \"Composants\\Périphériques à problème\"", true);
                string tooltipmessage = "";
                try
                {
                    foreach (string line in list)
                    {
                        if (line != "")
                        {
                            Log(3, line, true);
                            tooltipmessage += line + "\r\n";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log(3, "Erreur sur la recherche de pérphiques à problèmes. " + ex.Message, true);
                    ex.GetType();
                }
                toolTip.SetToolTip(pbWarning6, "Resultat de la comparaison du fichier ...export_NFO.txt du distributeur est : " + result.ToString() + "\r\nIl ne doit pas y avoir de périphériques à problème.\r\nConsultez le fichier NFO du distributeur dans la rubrique\r\n\"Composants\\Périphériques à problème\"\r\n" + tooltipmessage);
                pbWarning6.Visible = true;
            }
            Log(0, "Comparaison des dossiers DeploymentLogs", false);
            Log(0, "", true);
            pbar.Value = 64;
            isFailed = true;
            Cursor.Current = Cursors.WaitCursor;
            if (CompareFolders(Path.Combine(pathSNCF, @"DeploymentLogs"), Path.Combine(pathDIST, @"DeploymentLogs"), ""))
            {
                result = true;
                Log(0, "Resultat de la comparaison des dossiers DeploymentLogs est : " + result.ToString(), true);
                Application.DoEvents();
                pNeutral4.Visible = false;
                pSucces4.Visible = true;
                pSucces4.BringToFront();
                Application.DoEvents();
            }
            else
            {
                result = false;
                Application.DoEvents();
                pNeutral4.Visible = false;
                pFailed4.Visible = true;
                pFailed4.BringToFront();
                toolTip.SetToolTip(pFailed4, "Les deux dossiers doivent avoir le même nombre de fichiers \r\navec les mêmes noms. Dans le fichier de sortie,\r\nvous ne devez pas avoir de fichier non commun");
                Log(3, "Resultat de la comparaison des dossiers DeploymentLogs est : " + result.ToString(), true);
                Log(3, "Les deux dossiers doivent avoir le même nombre de fichiers avec les mêmes noms. Dans le fichier de sortie, vous ne devez pas avoir de fichier non commun", true);
                if (isFailed)
                {
                    toolTip.SetToolTip(pbWarning4, "Resultat de la comparaison des dossiers DeploymentLogs est : " + result.ToString() + "\r\nLes deux dossiers doivent avoir le même nombre de fichiers avec les mêmes noms.\r\nDans le fichier de sortie, vous ne devez pas avoir de fichier non commun\r\n" + fileDiffer);
                    pbWarning4.Visible = true;
                }
                fileDiffer = "";
                Application.DoEvents();
            }
            pbar.Value = 71;
            Log(0, "Comparaison des dossiers racines", false);
            Log(0, "", true);
            isFailed = true;
            Cursor.Current = Cursors.WaitCursor;
            if (CompareFolders(pathSNCF, pathDIST, "DeploymentLogs"))
            {
                result = true;
                Log(0, "Resultat de la comparaison des dossiers racine est : " + result.ToString(), true);
                Application.DoEvents();
                pNeutral5.Visible = false;
                pSucces5.Visible = true;
                pSucces5.BringToFront();
                Application.DoEvents();
            }
            else
            {
                if (isFailed)
                {
                    result = false;
                    Application.DoEvents();
                    pNeutral5.Visible = false;
                    pFailed5.Visible = true;
                    pFailed5.BringToFront();
                    toolTip.SetToolTip(pFailed5, "Les deux dossiers doivent avoir le même nombre de fichiers \r\navec les mêmes noms, sauf ceux identifiés avec le nom de la machine. Dans le fichier de sortie,\r\nvous ne devez pas avoir de fichier non commun.\r\nVeuillez consulter le fichier de sortie.");
                    Log(3, "Resultat de la comparaison des dossiers racine est : " + result.ToString(), true);
                    Log(3, "Les deux dossiers doivent avoir le même nombre de fichiers avec les mêmes noms, sauf ceux identifiés avec le nom de la machine. Dans le fichier de sortie, vous ne devez pas avoir de fichier non commun", true);
                    toolTip.SetToolTip(pbWarning5, "Resultat de la comparaison des dossiers racine est : " + result.ToString() + "\r\nLes deux dossiers doivent avoir le même nombre de fichiers avec les mêmes noms,\r\nsauf ceux identifiés avec le nom de la machine.\r\nDans le fichier de sortie, vous ne devez pas avoir de fichier non commun\r\n" + fileDiffer);
                    pbWarning5.Visible = true;
                    Application.DoEvents();
                }
                else
                {
                    result = true;
                    Log(0, "Resultat de la comparaison des dossiers racine est : " + result.ToString(), true);
                    Application.DoEvents();
                    pNeutral5.Visible = false;
                    pSucces5.Visible = true;
                    pSucces5.BringToFront();
                    Application.DoEvents();
                }
                fileDiffer = "";
            }
            Log(0, "Comparaison des fichiers Winsat", false);
            Log(0, "", true);
            Cursor.Current = Cursors.WaitCursor;
            result = await Task.Run(() =>
            {
                return thiscompare.CompareFile(Path.Combine(pathSNCF, @"WinSAT\indice_performance.xml"), Path.Combine(pathDIST, @"WinSAT\indice_performance.xml"));
            });
            pbar.Value = 88;
            if (result)
            {
                Log(0, "Resultat de la comparaison des fichiers indice_performance.xml est : " + result.ToString(), true);
                Application.DoEvents();
                pNeutral7.Visible = false;
                pSucces7.Visible = true;
                pSucces7.BringToFront();
                Application.DoEvents();
            }
            else
            {
                Application.DoEvents();
                string perfSNCF = "0";
                string perfDIST = "0";
                XDocument doc1 = XDocument.Load(Path.Combine(pathSNCF, @"WinSAT\indice_performance.xml"));
                foreach (XElement element in doc1.Element("WinSAT").Element("WinSPR").Descendants())
                {
                    if (element.Name == "SystemScore")
                        perfSNCF = element.Value;
                }
                XDocument doc2 = XDocument.Load(Path.Combine(pathDIST, @"WinSAT\indice_performance.xml"));
                foreach (XElement element in doc2.Element("WinSAT").Element("WinSPR").Descendants())
                {
                    if (element.Name == "SystemScore")
                        perfDIST = element.Value;
                }
                pNeutral7.Visible = false;
                pFailed7.Visible = true;
                pFailed7.BringToFront();
                toolTip.SetToolTip(pFailed7, "Il peut y avoir une legère différence sur l'élément WinSAT\\WinSPR\\SystemScore.");
                Application.DoEvents();
                Log(3, "Resultat de la comparaison des fichiers indice_performance.xml est : " + result.ToString(), true);
                Log(3, "Il peut y avoir une legère différence sur l'élément WinSAT\\WinSPR\\SystemScore.", true);
                Log(3, "Indice de performance du DISTRIBUTEUR WinSAT\\WinSPR\\SystemScore : " + perfDIST, true);
                Log(3, "Indice de performance de SNCF WinSAT\\WinSPR\\SystemScore : " + perfSNCF, true);
                Log(3, "Pour plus d'information, consulter les fichiers distributeur et SNCF", true);
                toolTip.SetToolTip(pbWarning7, "Resultat de la comparaison des fichiers indice_performance.xml est : " + result.ToString() + "\r\nIl peut y avoir une legère différence sur l'élément WinSAT\\WinSPR\\SystemScore.\r\nLe score distributeur ne doit pas être de beaucoup différent à celui de la SNCF.\r\nIndice distributeur : " + perfDIST + "\r\nIndice SNCF : " + perfSNCF);
                pbWarning7.Visible = true;
            }
            pbar.Value = 100;
            Log(0, "--- FIN DE LA RECETTE AUTOMATIQUE DISTRIBUTEUR, A VOUS DE JOUER ---", false);
            Cursor.Current = Cursors.Default;
            if (MessageBox.Show("La certification de la recette est terminée.\r\nVoulez-vous visualiser le fichier résultat collecté ?", "Fin de la certification", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (File.Exists(txtResult))
                {
                    openFile(txtResult);
                }
            }
            pbar.Value = 0;
        }

        private string[] FileToArray(string path)
        {
            var result = new List<string>();
            string[] fileLines = File.ReadAllLines(path);
            foreach(string line in fileLines)
            {
                result.Add(line);
            }
            return result.ToArray();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private void pictureBox12_Click(object sender, EventArgs e)
        {
            panel7.Visible = !panel7.Visible;
            panel7.BringToFront();
        }

        private void bR7SNCF7_Click(object sender, EventArgs e)
        {
            openFile(Path.Combine(pathSNCF, @"WinSAT\indice_performance.xml"));
        }

        private void bDist7_Click(object sender, EventArgs e)
        {
            openFile(Path.Combine(pathDIST, @"WinSAT\indice_performance.xml"));
        }

        private void cblDist_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void bAddDist_Click(object sender, EventArgs e)
        {
            if (txtNewDist.Text != "")
            {
                cblDist.Items.Add(txtNewDist.Text.ToUpper());
                listDistributeurs = "";
                foreach (string dist in cblDist.Items)
                {
                    listDistributeurs += dist + ";";
                }
                Registry.SetValue(HKCUValidRecette, "ListDistributeur", listDistributeurs.TrimEnd(';'), RegistryValueKind.String);
                txtNewDist.Text = "";
                BindCbDistributeurs();
            }
        }

        private void bRemoveDist_Click(object sender, EventArgs e)
        {
            while (cblDist.CheckedItems.Count > 0)
            {
                cblDist.Items.RemoveAt(cblDist.CheckedIndices[0]);
            }
            listDistributeurs = "";
            foreach (string dist in cblDist.Items)
            {
                listDistributeurs += dist + ";";
            }
            Registry.SetValue(HKCUValidRecette, "ListDistributeur", listDistributeurs.TrimEnd(';'), RegistryValueKind.String);
            BindCbDistributeurs();
        }

        private void cblFiles_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void rbWin7_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (rbWin7.Checked)
                {
                    Log(0, "Initialisation de la recherche des fichiers pour Windows 7", false);
                    cblFiles.Items.Clear();
                    try
                    {
                        if (txtRacineFolder.Text != "" && Directory.Exists(txtRacineFolder.Text))
                        {
                            string rootSubFolder = LastFolderFor(txtRacineFolder.Text);
                            try
                            {
                                if (rootSubFolder != "")
                                {
                                    string[] aSubFolderFiles = Directory.GetFiles(rootSubFolder, "*.doc?");

                                    foreach (string filename in aSubFolderFiles)
                                    {
                                        FileInfo fi = new FileInfo(filename);
                                        changefinish = true;
                                        txtBiggerDirFullName.Text = fi.DirectoryName;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new Exception(ex.Message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log(3, "Echec de la recherche des fichiers doc dans le dossier " + txtRacineFolder.Text + " : " + ex.Message, false);
                        throw new Exception(ex.Message);
                    }

                }
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private void rbWin10_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (rbWin10.Checked)
                {
                    try
                    {
                        Log(0, "Initialisation de la recherche des fichiers pour Windows 10", false);
                        cblFiles.Items.Clear();
                        if (txtRacineFolder.Text != "" && Directory.Exists(txtRacineFolder.Text))
                        {
                            string rootSubFolder = LastFolderFor(txtRacineFolder.Text);
                            try
                            {
                                if (rootSubFolder != "")
                                {
                                    string[] aSubFolderFiles = Directory.GetFiles(rootSubFolder, "*", SearchOption.TopDirectoryOnly);

                                    foreach (string filename in aSubFolderFiles)
                                    {
                                        FileInfo fi = new FileInfo(filename);
                                        changefinish = true;
                                        txtBiggerDirFullName.Text = fi.DirectoryName;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new Exception(ex.Message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log(3, "Echec de la recherche des fichiers doc dans le dossier " + txtRacineFolder.Text + " : " + ex.Message, false);
                        throw new Exception(ex.Message);
                    }

                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (txtBiggerDirFullName.Text != "")
            {
                fbd.SelectedPath = txtBiggerDirFullName.Text;
            }
            else
            {
                fbd.SelectedPath = null;
            }

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                changefinish = false;
                Log(0, "Recherche des fichiers doc dans le dossier " + fbd.SelectedPath, false);
                try
                {
                    if (fbd.SelectedPath != "" && Directory.Exists(fbd.SelectedPath))
                    {
                        string rootSubFolder = fbd.SelectedPath;
                        try
                        {
                            string[] aSubFolderFiles = Directory.GetFiles(rootSubFolder, "*.doc?");
                            if (aSubFolderFiles.Count() > 0)
                            {
                                if (rootSubFolder.Contains(" 15."))
                                {
                                    rbWin10.Checked = true;
                                } else
                                {
                                    rbWin7.Checked = true;
                                }
                                cblFiles.Items.Clear();
                                string changefolder = "";
                                foreach (string filename in aSubFolderFiles)
                                {
                                    FileInfo fi = new FileInfo(filename);
                                    if (fi.Extension.ToLower() == ".doc" || fi.Extension.ToLower() == ".docx")
                                    {
                                        cblFiles.Items.Add(fi.Name);
                                        changefolder = fi.DirectoryName;
                                    }
                                }
                                txtBiggerDirFullName.Text = changefolder;
                            } else
                            {
                                MessageBox.Show("Aucun fichier Microsoft Word dnas le dossier choisi.", "Absence de fichier", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log(3, "Echec de la recherche des fichiers doc dans le dossier " + fbd.SelectedPath + " : " + ex.Message, false);
                    throw new Exception(ex.Message);
                }
            }
        }

        private void txtBiggerDirFullName_TextChanged(object sender, EventArgs e)
        {
            if (changefinish)
            {
                changefinish = false;
                cblFiles.Items.Clear();
                Log(0, "Recherche des fichiers doc dans le dossier " + txtRacineFolder.Text, false);
                try
                {
                    if (txtRacineFolder.Text != "" && Directory.Exists(txtRacineFolder.Text))
                    {
                        string rootSubFolder = LastFolderFor(txtRacineFolder.Text);
                        try
                        {
                            string[] aSubFolderFiles = Directory.GetFiles(rootSubFolder, "*", SearchOption.TopDirectoryOnly);
                            string changefolder = "";
                            foreach (string filename in aSubFolderFiles)
                            {
                                FileInfo fi = new FileInfo(filename);
                                if (fi.Extension.ToLower() == ".doc" || fi.Extension.ToLower() == ".docx")
                                {
                                    cblFiles.Items.Add(fi.Name);
                                    changefolder = fi.DirectoryName;
                                }
                            }
                            txtBiggerDirFullName.Text = changefolder;
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log(3, "Echec de la recherche des fichiers doc dans le dossier " + txtRacineFolder.Text + " : " + ex.Message, false);
                    throw new Exception(ex.Message);
                }
            }
        }

        private void ChangeMasterCB()
        {
            initPicture();
            if (cbSNCFR7.Text == "")
            {
                bR7SNCF1.Enabled = false;
                bR7SNCF2.Enabled = false;
                bR7SNCF3.Enabled = false;
                bR7SNCF4.Enabled = false;
                bR7SNCF5.Enabled = false;
                bR7SNCF6.Enabled = false;
                bR7SNCF7.Enabled = false;
                bReadySNCF = false;
            }
            else
            {
                if (File.Exists(pathSNCF + @"\Version_Master.txt"))
                {
                    bR7SNCF1.Enabled = true;
                    bR7SNCF2.Enabled = true;
                    bR7SNCF3.Enabled = true;
                    bR7SNCF4.Enabled = true;
                    bR7SNCF5.Enabled = true;
                    bR7SNCF6.Enabled = true;
                    bR7SNCF7.Enabled = true;
                    bReadySNCF = true;
                }
                else
                {
                    bR7SNCF1.Enabled = false;
                    bR7SNCF2.Enabled = false;
                    bR7SNCF3.Enabled = false;
                    bR7SNCF4.Enabled = false;
                    bR7SNCF5.Enabled = false;
                    bR7SNCF6.Enabled = false;
                    bR7SNCF7.Enabled = false;
                    bReadySNCF = false;
                }
            }
            if (bReadyDist && bReadySNCF)
            {
                bCompare.Enabled = true;
            }
            else
            {
                bCompare.Enabled = false;
            }
        }

        private void cbMasters_TextChanged(object sender, EventArgs e)
        {
        }

        private void cbDistributeurs_TextChanged(object sender, EventArgs e)
        {
        }

        private void ChangeDistributeurCB()
        {
            initPicture();
            if (cbDISTR7.Text == "")
            {
                bDist1.Enabled = false;
                bDist2.Enabled = false;
                bDist3.Enabled = false;
                bDist4.Enabled = false;
                bDist5.Enabled = false;
                bDist6.Enabled = false;
                bDist7.Enabled = false;
                bReadyDist = false;
            }
            else
            {
                if (File.Exists(pathDIST + @"\Version_Master.txt"))
                {
                    bDist1.Enabled = true;
                    bDist2.Enabled = true;
                    bDist3.Enabled = true;
                    bDist4.Enabled = true;
                    bDist5.Enabled = true;
                    bDist6.Enabled = true;
                    bDist7.Enabled = true;
                    bReadyDist = true;
                }
                else
                {
                    bDist1.Enabled = false;
                    bDist2.Enabled = false;
                    bDist3.Enabled = false;
                    bDist4.Enabled = false;
                    bDist5.Enabled = false;
                    bDist6.Enabled = false;
                    bDist7.Enabled = false;
                    bReadyDist = false;
                }
            }
            if (bReadyDist && bReadySNCF)
            {
                bCompare.Enabled = true;
            }
            else
            {
                bCompare.Enabled = false;
            }
        }
        private void bBrowseRootFolder_Click_1(object sender, EventArgs e)
        {
            if(txtRacineFolder.Text != "")
            {
                fbd.SelectedPath = txtRacineFolder.Text;
            } else
            {
                fbd.SelectedPath = null;
            }

            if(fbd.ShowDialog() == DialogResult.OK)
            {
                txtRacineFolder.Text = fbd.SelectedPath;
                BindCbMasters();
            }
        }

        private void cbMasters_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                cbMasters.Text = cbMasters.SelectedItem.ToString();
                pathSNCF = Path.Combine(txtRacineFolder.Text, cbMasters.Text + @"\Recettes\SNCF");
                lastPathSNCF = pathSNCF;
                if (Directory.Exists(Path.Combine(txtRacineFolder.Text, cbMasters.Text + @"\Recette\SNCF")))
                {
                    pathSNCF = Path.Combine(txtRacineFolder.Text, cbMasters.Text + @"\Recette\SNCF");
                    lastPathSNCF = pathSNCF;
                }
                pathResult = lastPathSNCF;
                pathDIST = Path.Combine(txtRacineFolder.Text, cbMasters.Text + @"\Recettes\" + cbDistributeurs.Text);
                lastPathDIST = pathDIST;
                if (Directory.Exists(Path.Combine(txtRacineFolder.Text, cbMasters.Text + @"\Recette\" + cbDistributeurs.Text)))
                {
                    pathDIST = Path.Combine(txtRacineFolder.Text, cbMasters.Text + @"\Recette\" + cbDistributeurs.Text);
                    lastPathDIST = pathDIST;
                }
                Registry.SetValue(HKCUValidRecette, "lastMaster", cbMasters.Text, RegistryValueKind.String);
                BindCBSNCFR7();
                if(cbDistributeurs.Text != "")
                    BindCBDISTR7();

            }
            catch (Exception ex)
            {
                Log(3, "Erreur non gérée = " + ex.Message, false);
                ex.GetType();
            }
        }

        private void cbDistributeurs_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                cbDistributeurs.Text = cbDistributeurs.SelectedItem.ToString();
                pathDIST = Path.Combine(txtRacineFolder.Text, cbMasters.Text + @"\Recettes\" + cbDistributeurs.Text);
                lastPathDIST = pathDIST;
                if (Directory.Exists(Path.Combine(txtRacineFolder.Text, cbMasters.Text + @"\Recette\" + cbDistributeurs.Text)))
                {
                    pathDIST = Path.Combine(txtRacineFolder.Text, cbMasters.Text + @"\Recette\" + cbDistributeurs.Text);
                    lastPathDIST = pathDIST;
                }
                Registry.SetValue(HKCUValidRecette, "LastDistributeur", cbDistributeurs.Text, RegistryValueKind.String);
                BindCBDISTR7();
                BindCBSNCFR7();
            }
            catch (Exception ex)
            {
                Log(3, "Erreur non gérée = " + ex.Message, false);
                ex.GetType();
            }
        }

        private void cbSNCFR7_SelectedIndexChanged(object sender, EventArgs e)
        {
            pbar.Value = 0;
            try
            {
                pathSNCF = Path.Combine(lastPathSNCF, cbSNCFR7.SelectedItem.ToString());
                ChangeMasterCB();
                AdjustWidthComboBox_DropDown(sender, e);
            }
            catch (Exception ex)
            {
                Log(3, "Erreur non gérée = " + ex.Message, false);
                ex.GetType();
            }
        }

        private void cbDISTR7_SelectedIndexChanged(object sender, EventArgs e)
        {
            pbar.Value = 0;
            try
            {
                pathDIST = Path.Combine(lastPathDIST, cbDISTR7.SelectedItem.ToString());
                ChangeDistributeurCB();
                AdjustWidthComboBox_DropDown(sender, e);
            }
            catch (Exception ex)
            {
                Log(3, "Erreur non gérée = " + ex.Message, false);
                ex.GetType();
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            BindCBDISTR7();
            BindCBSNCFR7();
        }

        private void cbDISTR7_DropDown(object sender, EventArgs e)
        {
            AdjustWidthComboBox_DropDown(sender, e);
        }

        private void cbSNCFR7_DropDown(object sender, EventArgs e)
        {
            AdjustWidthComboBox_DropDown(sender, e);
        }

        private void pbWarning7_Click(object sender, EventArgs e)
        {

        }
    }

    public static class EnumerableExtensions
    {
        public static IEnumerable<T> Append<T>(
            this IEnumerable<T> source, params T[] tail)
        {
            return source.Concat(tail);
        }
    }

    class FileCompare : IEqualityComparer<FileInfo>
    {
        public FileCompare() { }

        public bool Equals(FileInfo f1, FileInfo f2)
        {
            return (f1.Name == f2.Name);
        }

        public bool CompareFile(string file1, string file2)
        {
            try
            {
                int file1byte;
                int file2byte;
                FileStream fs1;
                FileStream fs2;

                fs1 = new FileStream(file1, FileMode.Open);
                fs2 = new FileStream(file2, FileMode.Open);

                do
                {
                    // Read one byte from each file.
                    file1byte = fs1.ReadByte();
                    file2byte = fs2.ReadByte();
                }
                while ((file1byte == file2byte) && (file1byte != -1));

                // Close the files.
                fs1.Close();
                fs2.Close();

                return ((file1byte - file2byte) == 0);
            } catch(Exception ex)
            {
                return false;
                throw new Exception("Erreur lors de la comparaison des fichiers, " + ex.Message);
            }
        }

        public List<string> CompareBDDLogFile(string file1, string file2)
        {
            try
            {
                StreamReader fs1;
                StreamReader fs2;
                string line;

                fs1 = new StreamReader(file1);
                fs2 = new StreamReader(file2);

                List<string> af1 = new List<string>();
                List<string> af2 = new List<string>();
                string nline = "";
                int i = 0;
                while ((line = fs1.ReadLine()) != null)
                {
                    if (line.ToLower().Contains("failure"))
                    {
                        if (!line.Contains(@"\\") && !line.Contains("LTITriggerUpgradeFailure.wsf"))
                        {
                            nline = line.Split('>')[0] + ">";
                            af1.Add(nline);
                            i++;
                        }
                    }
                }
          
                nline = "";
                int x = 0;
                while ((line = fs2.ReadLine()) != null)
                {
                    if (line.ToLower().Contains("failure"))
                    {
                        if (!line.Contains(@"\\") && !line.Contains("LTITriggerUpgradeFailure.wsf"))
                        {
                            nline = line.Split('>')[0] + ">";
                            af2.Add(nline);
                            x++;
                        }
                    }
                }

                // Close the files.
                fs1.Close();
                fs2.Close();

                if (af2.Except(af1).ToList().Count() > 0)
                {
                    return af2.Except(af1).ToList();
                } else
                {
                    return new List<string>();
                }
            }
            catch (Exception ex)
            {
                return new List<string>();
                throw new Exception("Erreur lors de la comparaison des fichiers BDD.log, " + ex.Message);
            }
        }

        public int GetHashCode(FileInfo fi)
        {
            string s = string.Format("{0}", fi.Name);
            return s.GetHashCode();
        }
    }
}
