/* 
    Embedded Web Server App
    Copyright (C) 2012 Leonid Gordo

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace WebServer
{
	static class DateFormatter
	{
		public static string FormatDateTimeGmt(DateTime dt)
		{
			char[] chars = new char[20];
			Write2Chars(chars, 0, dt.Day);
			chars[2] = ' ';
			WriteMonthString(dt, chars, 3);
			chars[6] = ' ';
			Write4Chars(chars, 7, dt.Year);
			chars[11] = ' ';
			Write2Chars(chars, 12, dt.Hour);
			chars[14] = ':';
			Write2Chars(chars, 15, dt.Minute);
			chars[17] = ':';
			Write2Chars(chars, 18, dt.Second);

			return GetDayOfWeekString(dt) + ", " + new string(chars) + " GMT";
		}

		private static void Write2Chars(char[] chars, int offset, int value)
		{
			chars[offset] = Digit(value / 10);
			chars[offset + 1] = Digit(value % 10);
		}

		private static void Write4Chars(char[] chars, int offset, int value)
		{
			chars[offset++] = Digit(value / 1000);
			value = value % 1000;
			chars[offset++] = Digit(value / 100);
			value = value % 100;
			chars[offset++] = Digit(value / 10);
			chars[offset] = Digit(value % 10);
		}

		private static char Digit(int value)
		{
			return (char)(value + '0');
		}

		private static string GetDayOfWeekString(DateTime dt)
		{
			switch (dt.DayOfWeek)
			{
				case DayOfWeek.Monday:
					return "Mon";
				case DayOfWeek.Tuesday:
					return "Tue";
				case DayOfWeek.Wednesday:
					return "Wed";
				case DayOfWeek.Thursday:
					return "Thu";
				case DayOfWeek.Friday:
					return "Fri";
				case DayOfWeek.Saturday:
					return "Sat";
				case DayOfWeek.Sunday:
					return "Sun";
			}
			return string.Empty;
		}

		private static void WriteMonthString(DateTime dt, char[] mt, int index)
		{
			string month;
			switch (dt.Month)
			{
				case 1:
					month= "Jan";
					break;
				case 2:
					month = "Feb";
					break;
				case 3:
					month = "Mar";
					break;
				case 4:
					month = "Apr";
					break;
				case 5:
					month = "May";
					break;
				case 6:
					month = "Jun";
					break;
				case 7:
					month = "Jul";
					break;
				case 8:
					month = "Aug";
					break;
				case 9:
					month = "Sep";
					break;
				case 10:
					month = "Oct";
					break;
				case 11:
					month = "Nov";
					break;
				case 12:
					month = "Dec";
					break;
				default:
					month = string.Empty;
					break;
			}
			mt[index++] = month[0];
			mt[index++] = month[1];
			mt[index] = month[2];
		}

	}
}
