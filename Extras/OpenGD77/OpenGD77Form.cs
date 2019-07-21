using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace DMR
{
	public partial class OpenGD77Form : Form
	{
		private SerialPort _port = null;
//		private int data_start = 0;
//		private int data_length = 0;
//		private int data_pos = 0;
//		private commsDataMode data_mode = commsDataMode.DataModeNone;
//		private int data_sector = 0;
//		private byte[] dataBuff = null;

		private BackgroundWorker worker;
		private int old_progress = 0;
		public enum commsDataMode { DataModeNone = 0, DataModeReadFlash = 1, DataModeReadEEPROM = 2, DataModeWriteFlash = 3, DataModeWriteEEPROM = 4 };
		public enum taskCompleteAction { NONE, SAVE_EEPROM, SAVE_FLASH, SHOW_RESTORE_COMPLETE_MESSAGE,READ_CODEPLUG, WRITE_CODEPLUG }
		taskCompleteAction onTaskComplete = taskCompleteAction.NONE;

//		private int bufferDataPos = 0;
//		private int bufferDataTotal = 0;
		private SaveFileDialog _saveFileDialog = new SaveFileDialog();
		private OpenFileDialog _openFileDialog = new OpenFileDialog();
		private String _gd77CommPort;
//		int codeplugPhase = 0;
		private byte[] codeplugBuf;
		private byte[] codeplugBufEEPROM;
		private byte[] codeplugBufFlash;
		

		public OpenGD77Form()
		{
			InitializeComponent();
		}

		bool flashWritePrepareSector(int address, ref byte[] sendbuffer, ref byte[] readbuffer,CommsTransferData dataObj)
		{
			dataObj.data_sector = address / 4096;

			sendbuffer[0] = (byte)'W';
			sendbuffer[1] = 1;
			sendbuffer[2] = (byte)((dataObj.data_sector >> 16) & 0xFF);
			sendbuffer[3] = (byte)((dataObj.data_sector >> 8) & 0xFF);
			sendbuffer[4] = (byte)((dataObj.data_sector >> 0) & 0xFF);
			_port.Write(sendbuffer, 0, 5);
			while (_port.BytesToRead == 0)
			{
				Thread.Sleep(0);
			}
			_port.Read(readbuffer, 0, 64);

			return ((readbuffer[0] == sendbuffer[0]) && (readbuffer[1] == sendbuffer[1]));
		}

		bool flashSendData(int address, int len, ref byte[] sendbuffer, ref byte[] readbuffer)
		{
			sendbuffer[0] = (byte)'W';
			sendbuffer[1] = 2;
			sendbuffer[2] = (byte)((address >> 24) & 0xFF);
			sendbuffer[3] = (byte)((address >> 16) & 0xFF);
			sendbuffer[4] = (byte)((address >> 8) & 0xFF);
			sendbuffer[5] = (byte)((address >> 0) & 0xFF);
			sendbuffer[6] = (byte)((len >> 8) & 0xFF);
			sendbuffer[7] = (byte)((len >> 0) & 0xFF);
			_port.Write(sendbuffer, 0, len + 8);
			while (_port.BytesToRead == 0)
			{
				Thread.Sleep(0);
			}
			_port.Read(readbuffer, 0, 64);

			return ((readbuffer[0] == sendbuffer[0]) && (readbuffer[1] == sendbuffer[1]));
		}

		bool flashWriteSector(ref byte[] sendbuffer, ref byte[] readbuffer,CommsTransferData dataObj)
		{
			dataObj.data_sector = -1;

			sendbuffer[0] = (byte)'W';
			sendbuffer[1] = 3;
			_port.Write(sendbuffer, 0, 2);
			while (_port.BytesToRead == 0)
			{
				Thread.Sleep(0);
			}
			_port.Read(readbuffer, 0, 64);

			return ((readbuffer[0] == sendbuffer[0]) && (readbuffer[1] == sendbuffer[1]));
		}

		private void close_data_mode()
		{
			//data_mode = commsDataMode.DataModeNone;
		}

		private void ReadFlashOrEEPROM(CommsTransferData dataObj)
		{
			byte[] sendbuffer = new byte[512];
			byte[] readbuffer = new byte[512];
			byte[] com_Buf = new byte[256];

			int size = (dataObj.data_start + dataObj.data_length) - dataObj.data_pos;

			while (size > 0)
			{
				if (size > 32)
				{
					size = 32;
				}

				sendbuffer[0] = (byte)'R';
				sendbuffer[1] = (byte)dataObj.mode;
				sendbuffer[2] = (byte)((dataObj.data_pos >> 24) & 0xFF);
				sendbuffer[3] = (byte)((dataObj.data_pos >> 16) & 0xFF);
				sendbuffer[4] = (byte)((dataObj.data_pos >> 8) & 0xFF);
				sendbuffer[5] = (byte)((dataObj.data_pos >> 0) & 0xFF);
				sendbuffer[6] = (byte)((size >> 8) & 0xFF);
				sendbuffer[7] = (byte)((size >> 0) & 0xFF);
				_port.Write(sendbuffer, 0, 8);
				while (_port.BytesToRead == 0)
				{
					Thread.Sleep(0);
				}
				_port.Read(readbuffer, 0, 64);

				if (readbuffer[0] == 'R')
				{
					int len = (readbuffer[1] << 8) + (readbuffer[2] << 0);
					for (int i = 0; i < len; i++)
					{
						dataObj.dataBuff[dataObj.bufferDataPos++] = readbuffer[i + 3];
					}

					int progress = (dataObj.data_pos - dataObj.data_start) * 100 / dataObj.data_length;
					if (old_progress != progress)
					{
						updateProgess(dataObj.bufferDataPos, dataObj.bufferDataTotal);
						Console.WriteLine(progress + "%");
						old_progress = progress;
					}

					dataObj.data_pos = dataObj.data_pos + len;
				}
				else
				{
					Console.WriteLine(String.Format("read stopped (error at {0:X8})", dataObj.data_pos));
					close_data_mode();

				}
				size = (dataObj.data_start + dataObj.data_length) - dataObj.data_pos;
			}
			close_data_mode();
		}

		private void WriteFlash(CommsTransferData dataObj)
		{
			byte[] sendbuffer = new byte[512];
			byte[] readbuffer = new byte[512];
			byte[] com_Buf = new byte[256];

			int size = (dataObj.data_start + dataObj.data_length) - dataObj.data_pos;
			while (size > 0)
			{
				if (size > 32)
				{
					size = 32;
				}

				if (dataObj.data_sector == -1)
				{
					if (!flashWritePrepareSector(dataObj.data_pos, ref sendbuffer, ref readbuffer,dataObj))
					{
						close_data_mode();
						break;
					};
				}

				if (dataObj.mode != 0)
				{
					int len = 0;
					for (int i = 0; i < size; i++)
					{
						sendbuffer[i + 8] = dataObj.dataBuff[dataObj.bufferDataPos++];
						len++;

						if (dataObj.data_sector != ((dataObj.data_pos + len) / 4096))
						{
							break;
						}
					}
					if (flashSendData(dataObj.data_pos, len, ref sendbuffer, ref readbuffer))
					{
						int progress = (dataObj.data_pos - dataObj.data_start) * 100 / dataObj.data_length;
						if (old_progress != progress)
						{
							updateProgess(dataObj.bufferDataPos, dataObj.bufferDataTotal);
							old_progress = progress;
						}

						dataObj.data_pos = dataObj.data_pos + len;

						if (dataObj.data_sector != (dataObj.data_pos / 4096))
						{
							if (!flashWriteSector(ref sendbuffer, ref readbuffer,dataObj))
							{
								close_data_mode();
								break;
							};
						}
					}
					else
					{
						close_data_mode();
						break;
					}
				}
				size = (dataObj.data_start + dataObj.data_length) - dataObj.data_pos;
			}

			if (dataObj.data_sector != -1)
			{
				if (!flashWriteSector(ref sendbuffer, ref readbuffer,dataObj))
				{
					Console.WriteLine(String.Format("Error. Write stopped (write sector error at {0:X8})", dataObj.data_pos));
				};
			}

			close_data_mode();
		}

		private void WriteEEPROM(CommsTransferData dataObj)
		{
			byte[] sendbuffer = new byte[512];
			byte[] readbuffer = new byte[512];
			byte[] com_Buf = new byte[256];

			int size = (dataObj.data_start + dataObj.data_length) - dataObj.data_pos;
			while (size > 0)
			{
				if (size > 32)
				{
					size = 32;
				}

				if (dataObj.data_sector == -1)
				{
					dataObj.data_sector = dataObj.data_pos / 128;
				}

				int len = 0;
				for (int i = 0; i < size; i++)
				{
					sendbuffer[i + 8] = (byte)dataObj.dataBuff[dataObj.bufferDataPos++];
					len++;

					if (dataObj.data_sector != ((dataObj.data_pos + len) / 128))
					{
						dataObj.data_sector = -1;
						break;
					}
				}

				sendbuffer[0] = (byte)'W';
				sendbuffer[1] = 4;
				sendbuffer[2] = (byte)((dataObj.data_pos >> 24) & 0xFF);
				sendbuffer[3] = (byte)((dataObj.data_pos >> 16) & 0xFF);
				sendbuffer[4] = (byte)((dataObj.data_pos >> 8) & 0xFF);
				sendbuffer[5] = (byte)((dataObj.data_pos >> 0) & 0xFF);
				sendbuffer[6] = (byte)((len >> 8) & 0xFF);
				sendbuffer[7] = (byte)((len >> 0) & 0xFF);
				_port.Write(sendbuffer, 0, len + 8);
				while (_port.BytesToRead == 0)
				{
					Thread.Sleep(0);
				}
				_port.Read(readbuffer, 0, 64);

				if ((readbuffer[0] == sendbuffer[0]) && (readbuffer[1] == sendbuffer[1]))
				{
					int progress = (dataObj.data_pos - dataObj.data_start) * 100 / dataObj.data_length;
					if (old_progress != progress)
					{
						updateProgess(dataObj.bufferDataPos, dataObj.bufferDataTotal);
						Console.WriteLine(progress);
						old_progress = progress;
					}

					dataObj.data_pos = dataObj.data_pos + len;
				}
				else
				{
					Console.WriteLine(String.Format("Error. Write stopped (write sector error at {0:X8})", dataObj.data_pos));
					close_data_mode();
				}
				size = (dataObj.data_start + dataObj.data_length) - dataObj.data_pos;
			}
			close_data_mode();
		}

		void updateProgess(int bufferDataPos, int bufferDataTotal)
		{
			if (progressBar1.InvokeRequired)
				progressBar1.Invoke(new MethodInvoker(delegate()
				{
					progressBar1.Value = bufferDataPos*100 / bufferDataTotal;
				}));
			else
			{
				progressBar1.Value = bufferDataPos*100 / bufferDataTotal;
			}
		}

		void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (onTaskComplete != taskCompleteAction.NONE)
			{
				CommsTransferData dataObj = e.Result as CommsTransferData;

				switch (onTaskComplete)
				{
					case taskCompleteAction.SAVE_EEPROM:
						_saveFileDialog.Filter = "EEPROM files (*.bin)|*.bin";
						_saveFileDialog.FilterIndex = 1;
						if (_saveFileDialog.ShowDialog() == DialogResult.OK)
						{
							File.WriteAllBytes(_saveFileDialog.FileName, dataObj.dataBuff);
						}
						enableDisableAllButtons(true);
						onTaskComplete = taskCompleteAction.NONE;
						break;
					case taskCompleteAction.SAVE_FLASH:
						_saveFileDialog.Filter = "Flash files (*.bin)|*.bin";
						_saveFileDialog.FilterIndex = 1;
						if (_saveFileDialog.ShowDialog() == DialogResult.OK)
						{
							File.WriteAllBytes(_saveFileDialog.FileName, dataObj.dataBuff);
						}
						enableDisableAllButtons(true);
						onTaskComplete = taskCompleteAction.NONE;
						break;
					case taskCompleteAction.SHOW_RESTORE_COMPLETE_MESSAGE:
						MessageBox.Show("Restore complete");
						enableDisableAllButtons(true);
						onTaskComplete = taskCompleteAction.NONE;
						break;
					case taskCompleteAction.READ_CODEPLUG:
						break;
					case taskCompleteAction.WRITE_CODEPLUG:
						break;					
				}

				old_progress = 0;
			}
			progressBar1.Value = 0;
		}

		void worker_DoWork(object sender, DoWorkEventArgs e)
		{
			CommsTransferData dataObj = e.Argument as CommsTransferData;
			try
			{
				switch (dataObj.mode)
				{
					case commsDataMode.DataModeReadFlash:
					case commsDataMode.DataModeReadEEPROM:
						ReadFlashOrEEPROM(dataObj);
						break;

					case commsDataMode.DataModeWriteFlash:
						WriteFlash(dataObj);
						break;
					case commsDataMode.DataModeWriteEEPROM:
						WriteEEPROM(dataObj);
						break;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
			e.Result = dataObj;
		}

		void perFormCommsTask(CommsTransferData dataObj)
		{
			try
			{
				worker = new BackgroundWorker();
				worker.DoWork += new DoWorkEventHandler(worker_DoWork);
				worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
				worker.RunWorkerAsync(dataObj);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void btnBackupEEPROM_Click(object sender, EventArgs e)
		{
			if (_port==null)
			{
				MessageBox.Show("Please select a comm port");
				return;
			}
			CommsTransferData dataObj = new CommsTransferData();
			dataObj.mode = commsDataMode.DataModeReadEEPROM;

			onTaskComplete = taskCompleteAction.SAVE_FLASH;
			dataObj.bufferDataPos = 0;
			dataObj.data_pos = 0;
			dataObj.dataBuff = new Byte[64 * 1024];
			dataObj.data_start = 0;
			dataObj.data_length = 64*1024;
			dataObj.bufferDataTotal = 64 * 1024;
			enableDisableAllButtons(false);

			perFormCommsTask(dataObj);
		}

		private void btnBackupFlash_Click(object sender, EventArgs e)
		{
			if (_port==null)
			{
				MessageBox.Show("Please select a comm port");
				return;
			}

			CommsTransferData dataObj = new CommsTransferData();
			dataObj.mode = commsDataMode.DataModeReadFlash;
			dataObj.data_pos = 0;
			dataObj.bufferDataPos = 0;
			dataObj.dataBuff = new Byte[1024 * 1024];
			dataObj.data_start = 0;
			dataObj.data_length = 1024 * 1024;
			dataObj.bufferDataTotal = 1024 * 1024;
			onTaskComplete = taskCompleteAction.SAVE_FLASH;
			enableDisableAllButtons(false);
			perFormCommsTask(dataObj);

		}

		bool arrayCompare(byte[] buf1, byte[] buf2)
		{
			int len = Math.Min(buf1.Length, buf2.Length);

			for (int i=0; i<len; i++)
			{
				if (buf1[i]!=buf2[i])
				{
					return false;
				}
			}

			return true;
		}

		private void btnRestoreEEPROM_Click(object sender, EventArgs e)
		{
			if (_port == null)
			{
				MessageBox.Show("Please select a comm port");
				return;
			}
			if (DialogResult.Yes == MessageBox.Show("Are you sure you want to restore the EEPROM from a previously saved file?", "Warning", MessageBoxButtons.YesNo))
			{
				if (DialogResult.OK == _openFileDialog.ShowDialog())
				{
					CommsTransferData dataObj = new CommsTransferData();
					dataObj.dataBuff = File.ReadAllBytes(_openFileDialog.FileName);
					if (dataObj.dataBuff.Length == (64 * 1024))
					{
						byte []signature = {0x00 ,0x00 ,0x00 ,0x01 ,0x56 ,0x33 ,0x2E ,0x30 ,0x31};
						if (arrayCompare(dataObj.dataBuff, signature))
						{
							MessageBox.Show("Please set your radio into FM mode\nDo not press any buttons on the radio while the EEPROM is being restored");

							dataObj.mode = commsDataMode.DataModeWriteEEPROM;
							dataObj.data_pos = 0;
							dataObj.bufferDataPos = 0;
							dataObj.data_start = 0;
							dataObj.data_length = 64 * 1024;
							dataObj.bufferDataTotal = 64 * 1024;
							onTaskComplete = taskCompleteAction.SHOW_RESTORE_COMPLETE_MESSAGE;
							enableDisableAllButtons(false);
							perFormCommsTask(dataObj);

						}
						else
						{
							MessageBox.Show("The file does not start with the correct signature bytes", "Error");
						}
					}
					else
					{
						MessageBox.Show("The file is not the correct size.", "Error");
					}
				}
			}
		}

		private void btnRestoreFlash_Click(object sender, EventArgs e)
		{
			if (_port == null)
			{
				MessageBox.Show("Please select a comm port");
				return;
			}
			if (DialogResult.Yes == MessageBox.Show("Are you sure you want to restore the Flash memory from a previously saved file?", "Warning", MessageBoxButtons.YesNo))
			{
				if (DialogResult.OK == _openFileDialog.ShowDialog())
				{
					CommsTransferData dataObj = new CommsTransferData();

					dataObj.dataBuff = File.ReadAllBytes(_openFileDialog.FileName);
					if (dataObj.dataBuff.Length == (1024 * 1024))
					{
						byte[] signature = { 0x54, 0x59, 0x54, 0x3A, 0x4D, 0x44, 0x2D, 0x37, 0x36, 0x30 };
						if (arrayCompare(dataObj.dataBuff, signature))
						{
							MessageBox.Show("Please set your radio into FM mode\nDo not press any buttons on the radio while the Flash memory is being restored");


							dataObj.mode = commsDataMode.DataModeWriteFlash;

							dataObj.data_pos = 0;
							dataObj.bufferDataPos = 0;
							dataObj.data_start = 0;
							dataObj.data_length = 1024 * 1024;
							dataObj.bufferDataTotal = 1024 * 1024;
							onTaskComplete = taskCompleteAction.SHOW_RESTORE_COMPLETE_MESSAGE;
							dataObj.data_sector = -1;// Seems to be needed to force the first (4k ) page to be erased before it can be written
							enableDisableAllButtons(false);
							perFormCommsTask(dataObj);

						}
						else
						{
							MessageBox.Show("The file does not start with the correct signature bytes", "Error");
						}
					}
					else
					{
						MessageBox.Show("The file is not the correct size.", "Error");
					}
				}
			}
		}

		private void enableDisableAllButtons(bool show)
		{
			btnBackupEEPROM.Enabled = show;
			btnBackupFlash.Enabled = show;
			btnRestoreEEPROM.Enabled = show;
			btnRestoreFlash.Enabled = show;
		}

		private void OpenGD77Form_Load(object sender, EventArgs e)
		{
			_gd77CommPort = SetupDiWrap.ComPortNameFromFriendlyNamePrefix("OpenGD77");
			if (_gd77CommPort == null)
			{
				MessageBox.Show("Please connect the GD-77 running OpenGD77 firmware, and try again.", "OpenGD77 radio not detected.");
				this.Close();
			}
			else
			{
				try
				{
					_port = new SerialPort(_gd77CommPort, 115200, Parity.None, 8, StopBits.One);
					_port.ReadTimeout = 1000;
					_port.Open();
				}
				catch (Exception)
				{
					_port = null;
					MessageBox.Show("Failed to open comm port", "Error");
				}
			}
		}

		private void btnSelectCommPort_Click(object sender, EventArgs e)
		{
			if (_port == null)
			{
				try
				{
					_port = new SerialPort(comboBoxCOMPorts.Text, 115200, Parity.None, 8, StopBits.One);
					_port.ReadTimeout = 1000;
					_port.Open();
					(sender as Button).Text = "Close";
					comboBoxCOMPorts.Enabled = false;
				}
				catch (Exception)
				{
					_port = null;
					MessageBox.Show("Failed to open comm port", "Error");
				}
			}
			else
			{
				_port.Close();
				comboBoxCOMPorts.Enabled = true;
				(sender as Button).Text = "Select";
			}
		}

		void loadCOMPortlist()
		{
			string old_item = comboBoxCOMPorts.Text;
			comboBoxCOMPorts.Items.Clear();
			string[] ports = SerialPort.GetPortNames();
			foreach (string port in ports)
			{
				comboBoxCOMPorts.Items.Add(port);
			}
			if (comboBoxCOMPorts.Items.Contains(old_item))
			{
				comboBoxCOMPorts.Text = old_item;
			}
		}

		private void buttonRefreshCOMPortlist_Click(object sender, EventArgs e)
		{
			loadCOMPortlist();
		}

		private void OpenGD77Form_FormClosed(object sender, FormClosedEventArgs e)
		{
			_port.Close();
		}

		private void btnReadCodeplug_Click(object sender, EventArgs e)
		{

		}

		private void btnWriteCodeplug_Click(object sender, EventArgs e)
		{
			MessageBox.Show("btnWriteCodeplug_Click");
		}

		public class CommsTransferData
		{
			public commsDataMode mode;
			public int data_pos;
			public int data_start = 0;
			public int data_length = 0;
			public int bufferDataPos = 0;
			public int bufferDataTotal = 0;
			public int data_sector = 0;
			public byte[] dataBuff;
		}
	}


}
