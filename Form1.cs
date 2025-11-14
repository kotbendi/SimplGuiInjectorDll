using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
//Creator:Kotbendi
using System.Text;
namespace Injectbe
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.Text = "Injectbe v1.0.0";
            Uplodesave(listBox1); 
        }
        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr hObject);
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll")]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);
        const uint PROCESS_CREATE_THREAD = 0x0002;
        const uint PROCESS_QUERY_INFORMATION = 0x0400;
        const uint PROCESS_VM_OPERATION = 0x0008;
        const uint PROCESS_VM_WRITE = 0x0020;
        const uint PROCESS_VM_READ = 0x0010;

        
        const uint MEM_COMMIT = 0x00001000;
        const uint MEM_RESERVE = 0x00002000;
        const uint PAGE_READWRITE = 4;
        public static void Uplodesave(ListBox listBox1)
        {
            string pathsave = ("save.txt");
            if (File.Exists(pathsave))
            {
                string[] lines = File.ReadAllLines(pathsave);
                foreach (string line in lines)
                {
                    
                    listBox1.Items.Add(line);
                }
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                
                if (string.IsNullOrEmpty(textBox1.Text))
                {
                    MessageBox.Show("Enter name of process", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                
                if (listBox1.SelectedItem == null)
                {
                    MessageBox.Show("Select DLL from list", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string selected = listBox1.SelectedItem.ToString();

                
                if (!File.Exists(selected))
                {
                    MessageBox.Show("DLL file not found: " + selected, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                
                Process[] targetProcesses = Process.GetProcessesByName(textBox1.Text);
                if (targetProcesses.Length == 0)
                {
                    MessageBox.Show($"Process '{textBox1.Text}' not found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                Process targetProcess = targetProcesses[0];

                
                IntPtr procHandle = OpenProcess(PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION |
                                               PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ,
                                               false, targetProcess.Id);

                if (procHandle == IntPtr.Zero)
                {
                    MessageBox.Show("Failed to open process. Try running as Administrator.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                
                IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
                if (loadLibraryAddr == IntPtr.Zero)
                {
                    MessageBox.Show("Failed to get LoadLibraryA address", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    CloseHandle(procHandle);
                    return;
                }

                
                string dllPath = selected;
                uint memSize = (uint)(dllPath.Length + 1); 
                IntPtr allocMemAddress = VirtualAllocEx(procHandle, IntPtr.Zero, memSize,
                                                       MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);

                if (allocMemAddress == IntPtr.Zero)
                {
                    MessageBox.Show("Failed to allocate memory in target process", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    CloseHandle(procHandle);
                    return;
                }

                
                byte[] dllPathBytes = Encoding.ASCII.GetBytes(dllPath + "\0"); 
                bool writeResult = WriteProcessMemory(procHandle, allocMemAddress, dllPathBytes,
                                                     (uint)dllPathBytes.Length, out UIntPtr bytesWritten);

                if (!writeResult)
                {
                    MessageBox.Show("Failed to write DLL path to target process", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    CloseHandle(procHandle);
                    return;
                }

                
                IntPtr threadHandle = CreateRemoteThread(procHandle, IntPtr.Zero, 0, loadLibraryAddr,
                                                        allocMemAddress, 0, IntPtr.Zero);

                if (threadHandle == IntPtr.Zero)
                {
                    MessageBox.Show("Failed to create remote thread", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    CloseHandle(procHandle);
                    return;
                }

                // J. Закрытие хэндлов
                CloseHandle(threadHandle);
                CloseHandle(procHandle);

                MessageBox.Show("Injection successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
            "You wanna add dll to list?",
            "Question",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question
            );
            if (result == DialogResult.Yes)
            {
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    openFileDialog1.Filter = "DLL Files (*.dll)|*.dll";
                    openFileDialog1.Title = "Enter path to dll";
                    string filePath = openFileDialog1.FileName;
                    listBox1.Items.Add(filePath);
                }
            }
        }

        private void linkLabel1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("you can find name of game in task manager", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void label1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("To inject you need enter name of game and path to dll", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string pathsave = ("save.txt");
            Process currentProcess = Process.GetCurrentProcess();
            string fullPath = currentProcess.MainModule.FileName;
            MessageBox.Show($"Save..path{fullPath}", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (File.Exists(pathsave))
            {
                File.WriteAllText(pathsave, string.Empty);
                StringBuilder stringBuilder = new StringBuilder();
                // B. Перебираем все элементы ListBox
                foreach (var item in listBox1.Items)
                {
                    
                    stringBuilder.AppendLine(item.ToString());

                }
                File.WriteAllText(pathsave, stringBuilder.ToString());
            }
            else
            {
                StringBuilder stringBuilder = new StringBuilder();
                
                foreach (var item in listBox1.Items)
                {
                    
                    stringBuilder.AppendLine(item.ToString());
                }
                File.WriteAllText(pathsave, stringBuilder.ToString());
            }
        }
    }
}
