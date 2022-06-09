using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Timer = System.Windows.Forms.Timer;

namespace RealTimeGateway
{
    // Park disconnect
    // Work on update feature
    public struct patientData
    {
        public ProgressBar progressBar { get; set; }
        public Button status { get; set; }
        public Label name { get; set; }
        public Label imageCount { get; set; }
        public Label uploadTime { get; set; }
        public String storageNamespace { get; set; }
        public List<string> failedImages = new List<string>();
        //public Timer timer { get; set; }
    }
    public partial class GatewayOutput : Form
    {
        string version = "V1.0";
        Dictionary<string, patientData> uploadData;
        string path;
        FileInfo fileInfo;
        DateTime lastAccess;
        int lastMilliSeconds;
        int currentMilliseconds;
        string oldLine;
        string lastLine = null;
        Dictionary<string, List<string>> studyDictionary;
        private string sid;
        public GatewayOutput()
        {
            InitializeComponent();
            // Check if we need a "select your file path" - Cross bridge when we get there
            path = "C:/Program Files/Cimar/gateway/Logs/log.log";
            fileInfo = new FileInfo(path);
            lastAccess = fileInfo.LastWriteTime;
            lastMilliSeconds = 0;
            currentMilliseconds = 0;
            studyDictionary = new Dictionary<string, List<string>>();
            uploadData = new Dictionary<string, patientData>();
        }
        private async Task GetPatientData()
        {
            if(!Support.CheckInternetConnection())
            {
                Support.Print("There is no internet");
                return;
            }
            sid = await Task.Run(() => APICall.Login("loguser@cimar.co.uk", "uoX5lyzi%21qdgEVVY", Stack.cloud));
            if(sid == null)
            {
                return;
            }
            foreach (string studyID in uploadData.Keys)
            {
                JObject returnedStudies = await Task.Run(() => APICall.ReturnStudy(sid, studyID, Stack.cloud));
                if(returnedStudies == null)
                {
                    return;
                }
                foreach (var study in returnedStudies["studies"])
                {
                    if (uploadData[studyID].name.Text == "Pending Upload")
                    {
                        if (study["phi_namespace"].ToString() == study["storage_namespace"].ToString() && study["storage_namespace"].ToString() == uploadData[studyID].storageNamespace)
                        {
                            uploadData[studyID].name.Text = study["patient_name"].ToString();
                            uploadData[studyID].imageCount.Text = study["image_count"].ToString();
                            uploadData[studyID].uploadTime.Text = study["seconds_to_ingest"].ToString();

                            if(uploadData[studyID].progressBar.Maximum == 0)
                            {
                                StudyWasUploadedEarly(studyID, int.Parse(study["image_count"].ToString()));
                            }
                            else if(uploadData[studyID].progressBar.Value != uploadData[studyID].progressBar.Maximum)
                            {
                                foreach(string image in uploadData[studyID].failedImages)
                                {
                                    if(studyDictionary[studyID].Contains(image))
                                    {
                                        uploadData[studyID].failedImages.Remove(image);
                                    }
                                }
                                if(uploadData[studyID].failedImages.Count > 0)
                                {
                                    uploadData[studyID].status.BackColor = Color.Red;
                                    uploadData[studyID].status.Text = "Failed";
                                }
                            }
                        }
                    }
                    //else
                    //{
                    //    uploadData.Remove(studyID);
                    //    patientName.Stop();
                    //}
                }
            }
        }
        private void Tick_Tick(object sender, EventArgs e)
        {
            fileInfo.Refresh();
            if (lastAccess != fileInfo.LastWriteTime)
            {
                Tick.Stop();
                PrintUpdatedValue(fileInfo.LastWriteTime);
            }
        }

        private void StudyWasUploadedEarly(string study, int images)
        {
            uploadData[study].status.BackColor = Color.Green;
            SetProgressBarMax(study, images);
            uploadData[study].progressBar.Value = images;
        }

        private async Task PrintUpdatedValue(DateTime dt)
        {

            string newTime = dt.TimeOfDay.ToString();
            newTime = newTime.Remove(newTime.Length - 8, 8);
            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            using (StreamReader sr = new StreamReader(fs))
            {
                string line = null;

                while (sr.Peek() != -1)
                {                   
                    if(line == null)
                    {
                        line = await Task.Run(() => sr.ReadLineAsync());
                    }
                    if (line.Contains(DateTime.Now.Year.ToString()))
                    {
                        currentMilliseconds = ReturnCurrentMilliseconds(line);
                    }
                    // If the line does not contain this time then it will be ignored
                    if (!line.Contains(newTime))
                    {
                        if(lastMilliSeconds == 0)
                        {
                            // Reads the next line and reloops
                            line = await Task.Run(() => sr.ReadLineAsync());
                            continue;
                        }
                        else if (currentMilliseconds > lastMilliSeconds)
                        {
                            // We've found our line
                        }
                        else
                        {
                            // Reads the next line and reloops
                            line = await Task.Run(() => sr.ReadLineAsync());
                            continue;
                        }
                    }
                    
                    if (currentMilliseconds < lastMilliSeconds || oldLine == line || lastLine == line)
                    {
                        line = await Task.Run(() => sr.ReadLineAsync());
                        currentMilliseconds = ReturnCurrentMilliseconds(line);
                        continue;
                    }
                    else
                    {
                        if(line.Contains("Successful send to storage"))
                        {
                            string study = Support.GetStudyUID(Support.GetStudyAndImageUID(line));
                            if(studyDictionary.ContainsKey(study))
                            {
                                if (studyDictionary[study].Contains(Support.ReturnImageID(Support.GetStudyAndImageUID(line))))
                                {
                                    line = await Task.Run(() => sr.ReadLineAsync());
                                    continue;
                                }
                            }
                        }
                        lastMilliSeconds = currentMilliseconds;
                        oldLine = line;
                        if (line != null)
                        {
                            string returnValue = ProcessOutput(line, sr).ToString();
                            while(returnValue.Contains(DateTime.Now.Year.ToString()))
                            {
                                returnValue = ProcessOutput(returnValue, sr).ToString();
                                line = returnValue;
                                oldLine = line;
                                continue;
                            }
                        }                       
                        line = await Task.Run(() => sr.ReadLineAsync());
                        newTime = SetTime(line);
                    }
                }
                if (line != null && line.Contains(DateTime.Now.Year.ToString()))
                {
                    currentMilliseconds = ReturnCurrentMilliseconds(line);
                }
                oldLine = line;
                ProcessOutput(line, sr);
                fileInfo.Refresh();
                lastAccess = fileInfo.LastWriteTime;
                Tick.Start();
                lastMilliSeconds = currentMilliseconds;
            }
        }
        private int ReturnCurrentMilliseconds(string line)
        {
            string time = SetTime(line);
            return ReturnTotalMilliseconds(time);
        }
        private int ReturnTotalMilliseconds(string input)
        {
            return Support.RemoveComma(input);
        }
        private string SetTime(string input)
        {
            if(input == null)
            {
                return null;
            }
            string time = input.Substring(0, 24);
            time = time.Substring(11, 12);
            return time;
        }
        private void SetIdentifierColour(string study)
        {
            if (studyDictionary[study].Count() == uploadData[study].progressBar.Maximum)
            {
                uploadData[study].status.BackColor = Color.Green;
                uploadData[study].status.Text = "Complete";
            }
            else
            {
                if (uploadData[study].progressBar.Maximum != 0)
                {
                    uploadData[study].status.BackColor = Color.Red;
                    uploadData[study].status.Text = "Failed";
                }
            }
            patientName.Start();
        }

        // Process each line, determine what the output is
        private async Task<string> ProcessOutput(string line, StreamReader sr)
        {
            if(line == null)
            {
                return "NULL";
            }

            string study = Support.GetStudyUID(Support.GetStudyAndImageUID(line));
            study = study.Trim();

            switch(line)
            {
                case string a when a == null: 
                    return "NULL";
                case string b when b.Contains("Successful send to storage namespace"): 
                    SuccessfulSend(study, line);
                    return "Image Counted";
                case string c when c.Contains("Association starting"):
                    Support.Print("Association starting");
                    return "Association Starting";
                case string d when d.Contains("Association established"):
                    Support.Print("Association Established");
                    return "Association established";
                case string e when e.Contains("Association closing"):
                    AssociationClosing(line);
                    break;
                case string f when f.Contains("Sending study sync to storage to indicate upload completed for study"):
                    study = Support.GetStudyUIDWhenFinished(Support.GetStudyAndImageUID(line));
                    SetIdentifierColour(study);
                    break;
                case string g when g.Contains("ERROR"):
                    if(line.Contains("Exception sending file"))
                    {
                        ErrorSendingFile(line);
                    }
                    else if(line.Contains("The remote name could not be resolved"))
                    {
                        Support.Print("We reached here");
                    }

                    line = await Task.Run(() => sr.ReadLineAsync());
                    while (!line.Contains(DateTime.Now.Year.ToString()) && sr.Peek() != -1)
                    {
                        line = await Task.Run(() => sr.ReadLineAsync());
                    }

                    return line;
                default:
                    break;
            }
            return "Nothing Useful Found";
        }

        private void ErrorSendingFile(string line)
        {
            string study = Support.ReturnFailedStudy(line);
            if(!uploadData.Keys.Contains(study))
            {
                AddNewStudy(study);
            }
            if (!uploadData[study].failedImages.Contains(Support.ReturnFailedImage(line)))
            {
                uploadData[study].failedImages.Add(Support.ReturnFailedImage(line));
            }
        }
        private void AssociationClosing(string line)
        {
            string closingStudy = Support.GetStudyUIDWhenClosed(line);
            int totalImages = int.Parse(Support.FindTotalImageNumber(line));
            if (!studyDictionary.ContainsKey(closingStudy))
            {
                AddNewStudy(closingStudy, null);
            }
            SetProgressBarMax(closingStudy, totalImages);
        }
        private void SuccessfulSend(string study, string line)
        {
            if (!studyDictionary.ContainsKey(study))
            {
                AddNewStudy(study, Support.GetStorageNamespace(line));
            }
            if (uploadData[study].storageNamespace == null)
            {
                SetStorageNamespace(study, line);
            }
            if (!studyDictionary[study].Contains(Support.ReturnImageID(Support.GetStudyAndImageUID(line))))
            {
                studyDictionary[study].Add(Support.ReturnImageID(Support.GetStudyAndImageUID(line)));
                UpdateProgressBar(study);
            }
        }

        private void AddNewStudy(string study, string storageNamespace = null)
        {
            patientData data = new patientData();
            if(storageNamespace != null)
            {
                data.storageNamespace = storageNamespace.Trim();
            }
            studyDictionary.Add(study, new List<string>());
            {
                Label studyLabel = new Label();
                studyLabel.ForeColor = Color.White;
                studyLabel.Text = study;
                studyLabel.Width = IDWidth.Width;
                studyLabel.ImageAlign = ContentAlignment.MiddleLeft;
                studyLabel.Font = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Point);
                UIStudyStore.Controls.Add(studyLabel);
            }
            UIStudyStore.FlowDirection = FlowDirection.LeftToRight;
            {
                ProgressBar bar = new ProgressBar();
                bar.Name = study + "bar";
                bar.Maximum = 0;
                bar.Width = ProgressWidth.Width;
                bar.SetState(1);
                data.progressBar = bar;
                UIStudyStore.Controls.Add(bar);
            }
            {
                Label patientName = new Label();
                patientName.Text = "Pending Upload";
                patientName.ForeColor = Color.White;
                patientName.Font = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Point);
                patientName.Width = NameWidth.Width;
                patientName.Height = 25;
                patientName.Padding = new Padding(paddingWidth.Width, 0, 0, 0);
                data.name = patientName;
                UIStudyStore.Controls.Add(patientName);
            }
            {
                Label imageCount = new Label();
                imageCount.Text = "-";
                imageCount.ForeColor = Color.White;
                imageCount.Font = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Point);
                imageCount.Width = ImagesWidth.Width;
                imageCount.Height = 25;
                imageCount.Padding = new Padding(120, 0, 0, 0);
                data.imageCount = imageCount;
                UIStudyStore.Controls.Add(imageCount);
            }
            {
                Label timeLabel = new Label();
                timeLabel.Text = "-";
                timeLabel.ForeColor = Color.White;
                timeLabel.Font = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Point);
                timeLabel.Padding = new Padding(70, 0, 0, 0);
                timeLabel.Width = TimeWidth.Width;
                data.uploadTime = timeLabel;
                UIStudyStore.Controls.Add(timeLabel);
            }
            {
                Button progressButton = new Button();
                progressButton.BackColor = Color.Yellow;
                progressButton.Enabled = false;
                progressButton.FlatAppearance.BorderSize = 0;
                progressButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(60, 60, 60);
                progressButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 60, 60);
                progressButton.FlatStyle = FlatStyle.Flat;
                progressButton.Location = new Point(67, 154);
                progressButton.Name = "backgroundDownload";
                progressButton.Size = new Size(100, 30);
                progressButton.TabStop = false;
                progressButton.UseVisualStyleBackColor = true;
                progressButton.Text = "In Progress";
                progressButton.Font = new Font("Segoe UI", 8f, FontStyle.Bold, GraphicsUnit.Point);
                progressButton.TabStop = false;
                progressButton.Width = StatusWidth.Width;
                UIStudyStore.Controls.Add(progressButton);
                data.status = progressButton;
            }
            uploadData.Add(study, data);
        }

        private void UpdateProgressBar(string study)
        {
            if (uploadData[study].progressBar.Maximum != 0)
            {
                try
                {
                    uploadData[study].progressBar.Value = studyDictionary[study].Count();
                }
                catch (Exception ex)
                {
                    // do nothing, just stop the crash
                }

                if (uploadData[study].progressBar.Value >= uploadData[study].progressBar.Maximum)
                {
                    SetIdentifierColour(study);
                }
            }
        }

        private void SetProgressBarMax(string study, int totalImages)
        {
            uploadData[study].progressBar.Maximum += totalImages;
        }

        private void SetStorageNamespace(string study, string line)
        {
            patientData temp = uploadData[study];
            temp.storageNamespace = Support.GetStorageNamespace(line);
            uploadData[study] = temp;
        }

        private void patientName_Tick(object sender, EventArgs e)
        {
            GetPatientData();
        }
    }
}