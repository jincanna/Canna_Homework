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
        private int lastReadMessageId = -1;  // ���������� ���� �޽��� ID

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
            timer1.Interval = 100;  // 0.1�� �������� Timer ����
            timer1.Start();  // Timer ����
        }

        // Timer Tick �̺�Ʈ �ڵ鷯
        private void timer1_Tick_1(object sender, EventArgs e)
        {
            // Tick �̺�Ʈ�� �߻��� ������ �޽����� ���ΰ�ħ
            RefreshMessages();
        }

        // �����κ��� �޽����� �����ͼ� ȭ�鿡 ǥ���ϴ� �Լ�
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

        // �������� �ֽ� �޽����� ID�� �������� �Լ�
        private int GetLatestMessageIdFromServer(string url)
        {
            var data = GetResponseFromServer<Dictionary<string, int>>(url);
            return data != null ? data["latestMessageId"] : -1;
        }

        // �������� �޽����� �������� �Լ�
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

        // ��ư Ŭ�� �̺�Ʈ �ڵ鷯
        private void button1_Click(object sender, EventArgs e)
        {
            SendMessage(textBox2.Text);
            textBox2.Clear();  // �Է�â�� ���ϴ�.
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
