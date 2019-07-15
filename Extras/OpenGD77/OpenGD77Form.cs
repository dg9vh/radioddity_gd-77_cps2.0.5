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
		private SerialPort port = null;
		private int data_start = 0;
		private int data_length = 0;
		private int data_pos = 0;
		private commsDataMode data_mode = commsDataMode.DataModeNone;
		private int data_sector = 0;
	//	private Stream fileStream;
		private byte[] dataBuff = null;

		private bool running = false;
		private BackgroundWorker worker;
		private bool stop_worker = false;
		private int old_progress = 0;
		enum commsDataMode { DataModeNone = 0, DataModeReadFlash = 1, DataModeReadEEPROM = 2, DataModeWriteFlash = 3, DataModeWriteEEPROM = 4 };

		private int bufferDataPos = 0;
		private int bufferDataTotal = 0;

		public OpenGD77Form()
		{
			InitializeComponent();
			loadCOMPortlist();
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


		bool prepare_sector(int address, ref byte[] sendbuffer, ref byte[] readbuffer)
		{
			data_sector = address / 4096;

			sendbuffer[0] = (byte)'W';
			sendbuffer[1] = 1;
			sendbuffer[2] = (byte)((data_sector >> 16) & 0xFF);
			sendbuffer[3] = (byte)((data_sector >> 8) & 0xFF);
			sendbuffer[4] = (byte)((data_sector >> 0) & 0xFF);
			port.Write(sendbuffer, 0, 5);
			while (port.BytesToRead == 0)
			{
				Thread.Sleep(0);
			}
			port.Read(readbuffer, 0, 64);

			return ((readbuffer[0] == sendbuffer[0]) && (readbuffer[1] == sendbuffer[1]));
		}

		bool send_data(int address, int len, ref byte[] sendbuffer, ref byte[] readbuffer)
		{
			sendbuffer[0] = (byte)'W';
			sendbuffer[1] = 2;
			sendbuffer[2] = (byte)((address >> 24) & 0xFF);
			sendbuffer[3] = (byte)((address >> 16) & 0xFF);
			sendbuffer[4] = (byte)((address >> 8) & 0xFF);
			sendbuffer[5] = (byte)((address >> 0) & 0xFF);
			sendbuffer[6] = (byte)((len >> 8) & 0xFF);
			sendbuffer[7] = (byte)((len >> 0) & 0xFF);
			port.Write(sendbuffer, 0, len + 8);
			while (port.BytesToRead == 0)
			{
				Thread.Sleep(0);
			}
			port.Read(readbuffer, 0, 64);

			return ((readbuffer[0] == sendbuffer[0]) && (readbuffer[1] == sendbuffer[1]));
		}

		bool write_sector(ref byte[] sendbuffer, ref byte[] readbuffer)
		{
			data_sector = -1;

			sendbuffer[0] = (byte)'W';
			sendbuffer[1] = 3;
			port.Write(sendbuffer, 0, 2);
			while (port.BytesToRead == 0)
			{
				Thread.Sleep(0);
			}
			port.Read(readbuffer, 0, 64);

			return ((readbuffer[0] == sendbuffer[0]) && (readbuffer[1] == sendbuffer[1]));
		}

		private void close_data_mode()
		{
			data_mode = commsDataMode.DataModeNone;
			stop_worker = true;
		}

		void worker_DoWork(object sender, DoWorkEventArgs e)
		{
			byte[] sendbuffer = new byte[512];
			byte[] readbuffer = new byte[512];

			byte[] com_Buf = new byte[256];

			stop_worker = false;

			while (!stop_worker)
			{
				try
				{
					if ((data_mode == commsDataMode.DataModeReadFlash) || (data_mode == commsDataMode.DataModeReadEEPROM))
					{
						int size = (data_start + data_length) - data_pos;
						if (size > 0)
						{
							if (size > 32)
							{
								size = 32;
							}

							sendbuffer[0] = (byte)'R';
							sendbuffer[1] = (byte)data_mode;
							sendbuffer[2] = (byte)((data_pos >> 24) & 0xFF);
							sendbuffer[3] = (byte)((data_pos >> 16) & 0xFF);
							sendbuffer[4] = (byte)((data_pos >> 8) & 0xFF);
							sendbuffer[5] = (byte)((data_pos >> 0) & 0xFF);
							sendbuffer[6] = (byte)((size >> 8) & 0xFF);
							sendbuffer[7] = (byte)((size >> 0) & 0xFF);
							port.Write(sendbuffer, 0, 8);
							while (port.BytesToRead == 0)
							{
								Thread.Sleep(0);
							}
							port.Read(readbuffer, 0, 64);

							if (readbuffer[0] == 'R')
							{
								int len = (readbuffer[1] << 8) + (readbuffer[2] << 0);
								for (int i = 0; i < len; i++)
								{
									dataBuff[bufferDataPos++]=readbuffer[i + 3];
								}

								int progress = (data_pos - data_start) * 100 / data_length;
								if (old_progress != progress)
								{
									updateProgess();
									Console.WriteLine(progress+"%");
									old_progress = progress;
								}

								data_pos = data_pos + len;
							}
							else
							{
								Console.WriteLine(String.Format("read stopped (error at {0:X8})", data_pos));
								close_data_mode();

							}
						}
						else
						{
							Console.WriteLine("read finished");
							close_data_mode();
						}
					}
					else if (data_mode == commsDataMode.DataModeWriteFlash)
					{
						int size = (data_start + data_length) - data_pos;
						if (size > 0)
						{
							if (size > 32)
							{
								size = 32;
							}

							if (data_sector == -1)
							{
								if (!prepare_sector(data_pos, ref sendbuffer, ref readbuffer))
								{
									close_data_mode();
								};
							}

							if (data_mode != 0)
							{
								int len = 0;
								for (int i = 0; i < size; i++)
								{
									sendbuffer[i + 8] = dataBuff[bufferDataPos++];
									len++;

									if (data_sector != ((data_pos + len) / 4096))
									{
										break;
									}
								}

								if (send_data(data_pos, len, ref sendbuffer, ref readbuffer))
								{
									int progress = (data_pos - data_start) * 100 / data_length;
									if (old_progress != progress)
									{
										Console.WriteLine(progress);
										old_progress = progress;
									}

									data_pos = data_pos + len;

									if (data_sector != (data_pos / 4096))
									{
										if (!write_sector(ref sendbuffer, ref readbuffer))
										{
											close_data_mode();
										};
									}
								}
								else
								{
									close_data_mode();
								}
							}
						}
						else
						{
							if (data_sector != -1)
							{
								if (!write_sector(ref sendbuffer, ref readbuffer))
								{
									Console.WriteLine(String.Format("Error. Write stopped (write sector error at {0:X8})", data_pos));
								};
							}

							close_data_mode();
						}
					}
					else if (data_mode == commsDataMode.DataModeWriteEEPROM)
					{
						int size = (data_start + data_length) - data_pos;
						if (size > 0)
						{
							if (size > 32)
							{
								size = 32;
							}

							if (data_sector == -1)
							{
								data_sector = data_pos / 128;
							}

							int len = 0;
							for (int i = 0; i < size; i++)
							{
								sendbuffer[i + 8] = (byte)dataBuff[bufferDataPos++];
								len++;

								if (data_sector != ((data_pos + len) / 128))
								{
									data_sector = -1;
									break;
								}
							}

							sendbuffer[0] = (byte)'W';
							sendbuffer[1] = 4;
							sendbuffer[2] = (byte)((data_pos >> 24) & 0xFF);
							sendbuffer[3] = (byte)((data_pos >> 16) & 0xFF);
							sendbuffer[4] = (byte)((data_pos >> 8) & 0xFF);
							sendbuffer[5] = (byte)((data_pos >> 0) & 0xFF);
							sendbuffer[6] = (byte)((len >> 8) & 0xFF);
							sendbuffer[7] = (byte)((len >> 0) & 0xFF);
							port.Write(sendbuffer, 0, len + 8);
							while (port.BytesToRead == 0)
							{
								Thread.Sleep(0);
							}
							port.Read(readbuffer, 0, 64);

							if ((readbuffer[0] == sendbuffer[0]) && (readbuffer[1] == sendbuffer[1]))
							{
								int progress = (data_pos - data_start) * 100 / data_length;
								if (old_progress != progress)
								{
									Console.Write(progress);
									old_progress = progress;
								}

								data_pos = data_pos + len;
							}
							else
							{
								Console.WriteLine(String.Format("Error. Write stopped (write sector error at {0:X8})", data_pos));
								close_data_mode();
							}
						}
						else
						{
							Console.WriteLine("write finished");
							close_data_mode();
						}
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message);
					break;
				}

				if ((data_mode != commsDataMode.DataModeReadFlash) && (data_mode != commsDataMode.DataModeReadEEPROM) && (data_mode != commsDataMode.DataModeWriteFlash) && (data_mode != commsDataMode.DataModeWriteEEPROM))
				{
					Thread.Sleep(10);
				}
			}

		}

		void updateProgess()
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
			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
			switch (saveType)
			{
				case 0:
					saveFileDialog.Filter = "eeprom files (*.bin)|*.bin";
					break;
				case 1:
					saveFileDialog.Filter = "Flash files (*.bin)|*.bin";
					break;
			}

			progressBar1.Value = 0;
			saveFileDialog.FilterIndex = 1;
			saveFileDialog.RestoreDirectory = true;
			if (saveFileDialog.ShowDialog() == DialogResult.OK)
			{
				File.WriteAllBytes(saveFileDialog.FileName, dataBuff);
			}

			running = false;
		}



		private void btnSelectCommPort_Click(object sender, EventArgs e)
		{
			if (port == null)
			{
				try
				{
					port = new SerialPort(comboBoxCOMPorts.Text, 115200, Parity.None, 8, StopBits.One);
					port.ReadTimeout = 1000;
					port.Open();
					(sender as Button).Text = "Close";
					comboBoxCOMPorts.Enabled = false;
				}
				catch (Exception)
				{
					port = null;
					MessageBox.Show("Failed to open comm port", "Error");
				}
			}
			else
			{
				port.Close();
				comboBoxCOMPorts.Enabled = true;
				(sender as Button).Text = "Select";
			}
		}

		int saveType = 0;
		private void btnBackupEEPROM_Click(object sender, EventArgs e)
		{
			if (port==null)
			{
				MessageBox.Show("Please select a comm port");
				return;
			}
			saveType = 0;
			data_mode = commsDataMode.DataModeReadEEPROM;
			bufferDataPos = 0;
			
			dataBuff = new Byte[64 * 1024];
			data_start=0;
			data_length = 64*1024;
			bufferDataTotal = 64 * 1024;
			perFormCommsTask();
		}

		private void btnBackupFlash_Click(object sender, EventArgs e)
		{
			if (port==null)
			{
				MessageBox.Show("Please select a comm port");
				return;
			}

			data_mode = commsDataMode.DataModeReadFlash;
			data_pos = 0;
			bufferDataPos = 0;
			dataBuff = new Byte[1024 * 1024];
			data_start = 0;
			data_length = 1024 * 1024;
			bufferDataTotal = 1024 * 1024;
			saveType = 1;
			perFormCommsTask();
		}




		void perFormCommsTask()
		{
			try
			{
				worker = new BackgroundWorker();
				worker.DoWork += new DoWorkEventHandler(worker_DoWork);
				worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
				worker.RunWorkerAsync();

				running = true;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}


	}
}
