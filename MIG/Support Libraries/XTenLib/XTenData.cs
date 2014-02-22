/*
    This file is part of HomeGenie Project source code.

    HomeGenie is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    HomeGenie is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with HomeGenie.  If not, see <http://www.gnu.org/licenses/>.  
*/

/*
 *     Author: Generoso Martello <gene@homegenie.it>
 *     Project Homepage: http://homegenie.it
 */

using System;
using System.ComponentModel;

namespace XTenLib
{

	public class X10Module : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public string Description { get; set; }
		public string Code { get; set; }
		/*
		private string _status;
		public string Status { 
			get { return _status; }
			set { 
				_status = value;
				OnPropertyChanged("Status");
			}
		}
		*/
		private double _level;
		public double Level {
			get { return _level; }
			set { 
				_level = value;
				OnPropertyChanged ("Level");
			}
		}
		
		public X10Module()
		{
			Description = "";
			Code = "";
			//Status = "";
			Level = 0.0;
		}

		protected void OnPropertyChanged(string name)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(name));
			}
		}
	}
	
	public enum X10Command
	{
		All_Units_Off,
		All_Lights_On,
		On,
		Off,
		Dim,
		Bright,
		All_Lights_Off,
		Extended_Code,
		Hail_Request,
		Hail_Acknowledge,
		Preset_Dim_1,
		Preset_Dim_2,
		Extended_Data_transfer,
		Status_On,
		Status_Off,
		Status_Request
	}
	
	public enum X10CommandType
	{
		Address = 0x04,
		Function = 0x06,
        //
		PLC_Ready = 0x55,
		PLC_Poll = 0x5A,
		Macro = 0x5B,
		RF = 0x5D,
        //
        PLC_TimeRequest = 0xA5
	}
	
	public enum X10HouseCodes
	{
		A = 6,
		B = 14,
		C = 2,
		D = 10,
		E = 1,
		F = 9,
		G = 5,
		H = 13,
		I = 7,
		J = 15,
		K = 3,
		L = 11,
		M = 0,
		N = 8,
		O = 4,
		P = 12
	}
	
	public enum X10UnitCodes
	{
		Unit_1 = 6,
		Unit_2 = 14,
		Unit_3 = 2,
		Unit_4 = 10,
		Unit_5 = 1,
		Unit_6 = 9,
		Unit_7 = 5,
		Unit_8 = 13,
		Unit_9 = 7,
		Unit_10 = 15,
		Unit_11 = 3,
		Unit_12 = 11,
		Unit_13 = 0,
		Unit_14 = 8,
		Unit_15 = 4,
		Unit_16 = 12
	}

	public static class Utility
	{



		public static string HouseUnitCodeFromEnum(X10HouseCodes housecode, X10UnitCodes unitcodes)
		{
			string unit = unitcodes.ToString();
			unit = unit.Substring(unit.LastIndexOf("_") + 1);
			//
			return housecode.ToString() + unit;
		}
		
		public static X10HouseCodes HouseCodeFromString(string s)
		{
			X10HouseCodes hc = X10HouseCodes.A;
			s = s.Substring(0, 1).ToUpper();
			switch (s)
			{
			case "A":
				hc = X10HouseCodes.A;
				break;
			case "B":
				hc = X10HouseCodes.B;
				break;
			case "C":
				hc = X10HouseCodes.C;
				break;
			case "D":
				hc = X10HouseCodes.D;
				break;
			case "E":
				hc = X10HouseCodes.E;
				break;
			case "F":
				hc = X10HouseCodes.F;
				break;
			case "G":
				hc = X10HouseCodes.G;
				break;
			case "H":
				hc = X10HouseCodes.H;
				break;
			case "I":
				hc = X10HouseCodes.I;
				break;
			case "J":
				hc = X10HouseCodes.J;
				break;
			case "K":
				hc = X10HouseCodes.K;
				break;
			case "L":
				hc = X10HouseCodes.L;
				break;
			case "M":
				hc = X10HouseCodes.M;
				break;
			case "N":
				hc = X10HouseCodes.N;
				break;
			case "O":
				hc = X10HouseCodes.O;
				break;
			case "P":
				hc = X10HouseCodes.P;
				break;
			}
			return hc;
		}

		public static X10UnitCodes UnitCodeFromString(string s)
		{
			X10UnitCodes uc = X10UnitCodes.Unit_1;
			s = s.Substring(1);
			switch (s)
			{
			case "1":
				uc = X10UnitCodes.Unit_1;
				break;
			case "2":
				uc = X10UnitCodes.Unit_2;
				break;
			case "3":
				uc = X10UnitCodes.Unit_3;
				break;
			case "4":
				uc = X10UnitCodes.Unit_4;
				break;
			case "5":
				uc = X10UnitCodes.Unit_5;
				break;
			case "6":
				uc = X10UnitCodes.Unit_6;
				break;
			case "7":
				uc = X10UnitCodes.Unit_7;
				break;
			case "8":
				uc = X10UnitCodes.Unit_8;
				break;
			case "9":
				uc = X10UnitCodes.Unit_9;
				break;
			case "10":
				uc = X10UnitCodes.Unit_10;
				break;
			case "11":
				uc = X10UnitCodes.Unit_11;
				break;
			case "12":
				uc = X10UnitCodes.Unit_12;
				break;
			case "13":
				uc = X10UnitCodes.Unit_13;
				break;
			case "14":
				uc = X10UnitCodes.Unit_14;
				break;
			case "15":
				uc = X10UnitCodes.Unit_15;
				break;
			case "16":
				uc = X10UnitCodes.Unit_16;
				break;
			}
			return uc;
		}

		public static String ByteArrayToString(byte[] message)
		{
			String ret = String.Empty;
			foreach (byte b in message)
			{
				ret += b.ToString("X2") + " ";
			}
			return ret.Trim();
		}

	}

}

