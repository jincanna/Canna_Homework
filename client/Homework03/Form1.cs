using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace Homework03
{
    public partial class Form1 : Form
    {
        private int lastReadMessageId = -1;  // 마지막으로 읽은 메시지 ID

        private const int SB_VERT = 0x1;
        private const int SIF_ALL = 0x17;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetScrollInfo(IntPtr hWnd, int n, ref SCROLLINFO lpsi);

        private struct SCROLLINFO
        {
            public uint cbSize;
            public uint fMask;
            public int nMin;
            public int nMax;
            public uint nPage;
            public int nPos;
            public int nTrackPos;
        }

        public Form1()
        {
            InitializeComponent();
            timer1.Interval = 100;  // 0.1초 간격으로 Timer 설정
            timer1.Start();  // Timer 시작
        }

        // Timer Tick 이벤트 핸들러
        private void timer1_Tick_1(object sender, EventArgs e)
        {
            // Tick 이벤트가 발생할 때마다 메시지를 새로고침
            RefreshMessages();
        }

        // 서버로부터 메시지를 가져와서 화면에 표시하는 함수
        private void RefreshMessages()
        {
            var latestMessageId = GetLatestMessageIdFromServer("https://localhost:7194/GetLatestMessageId");
            if (latestMessageId > lastReadMessageId)
            {
                var messages = GetMessagesFromServer("https://localhost:7194/GetMessagesMoreThanOrEqual/" + (lastReadMessageId + 1));
                if (messages != null)
                {
                    var scrollInfoBefore = GetScrollInfo();

                    bool scrollBarAtBottom = IsScrollBarAtBottom(scrollInfoBefore);

                    foreach (var kv in messages)
                    {
                        string newMessage = (richTextBox1.Text == string.Empty ? "" : Environment.NewLine) + kv.Key + " : " + kv.Value;
                        richTextBox1.AppendText(newMessage);
                        lastReadMessageId = kv.Key;
                    }

                    var scrollInfoAfter = GetScrollInfo();

                    if (scrollBarAtBottom)
                    {
                        ScrollToBottom();
                    }
                }
            }
        }

        private SCROLLINFO GetScrollInfo()
        {
            SCROLLINFO si = new SCROLLINFO();
            si.cbSize = (uint)Marshal.SizeOf(si);
            si.fMask = SIF_ALL;
            GetScrollInfo(richTextBox1.Handle, SB_VERT, ref si);
            return si;
        }

        private bool IsScrollBarAtBottom(SCROLLINFO si)
        {
            return si.nPos + (int)si.nPage >= si.nMax;
        }

        private void ScrollToBottom()
        {
            richTextBox1.SelectionStart = richTextBox1.TextLength;
            richTextBox1.ScrollToCaret();
        }

        // 서버에서 최신 메시지의 ID를 가져오는 함수
        private int GetLatestMessageIdFromServer(string url)
        {
            var data = GetResponseFromServer<Dictionary<string, int>>(url);
            return data != null ? data["latestMessageId"] : -1;
        }

        // 서버에서 메시지를 가져오는 함수
        private Dictionary<int, string> GetMessagesFromServer(string url)
        {
            return GetResponseFromServer<Dictionary<int, string>>(url);
        }

        private T GetResponseFromServer<T>(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();

                var data = JsonSerializer.Deserialize<T>(responseFromServer);

                reader.Close();
                dataStream.Close();
                response.Close();

                return data;
            }
            catch (WebException ex) when ((ex.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
            {
                return default(T);
            }
        }

        // 버튼 클릭 이벤트 핸들러
        private void button1_Click(object sender, EventArgs e)
        {
            SendMessage(textBox2.Text);
            textBox2.Clear();  // 입력창은 비웁니다.
        }

        private void SendMessage(string message)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://localhost:7194/SendMessage");
            request.Method = "POST";

            var postData = new { Message = message };
            string json = JsonSerializer.Serialize(postData);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            request.ContentType = "application/json";
            request.ContentLength = byteArray.Length;

            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();

            reader.Close();
            dataStream.Close();
            response.Close();
        }
    }
}
