using System;

namespace Script.Methods
{
	// Token: 0x02000014 RID: 20
	public enum ShapeColor
	{
		// Token: 0x04000036 RID: 54
		None,
		// Token: 0x04000037 RID: 55
		AliceBlue,
		// Token: 0x04000038 RID: 56
		PaleGoldenrod,
		// Token: 0x04000039 RID: 57
		Orchid,
		// Token: 0x0400003A RID: 58
		OrangeRed,
		// Token: 0x0400003B RID: 59
		Orange,
		// Token: 0x0400003C RID: 60
		OliveDrab,
		// Token: 0x0400003D RID: 61
		Olive,
		// Token: 0x0400003E RID: 62
		OldLace,
		// Token: 0x0400003F RID: 63
		Navy,
		// Token: 0x04000040 RID: 64
		NavajoWhite,
		// Token: 0x04000041 RID: 65
		Moccasin,
		// Token: 0x04000042 RID: 66
		MistyRose,
		// Token: 0x04000043 RID: 67
		MintCream,
		// Token: 0x04000044 RID: 68
		MidnightBlue,
		// Token: 0x04000045 RID: 69
		MediumVioletRed,
		// Token: 0x04000046 RID: 70
		MediumTurquoise,
		// Token: 0x04000047 RID: 71
		MediumSpringGreen,
		// Token: 0x04000048 RID: 72
		MediumSlateBlue,
		// Token: 0x04000049 RID: 73
		LightSkyBlue,
		// Token: 0x0400004A RID: 74
		LightSlateGray,
		// Token: 0x0400004B RID: 75
		LightSteelBlue,
		// Token: 0x0400004C RID: 76
		LightYellow,
		// Token: 0x0400004D RID: 77
		Lime,
		// Token: 0x0400004E RID: 78
		LimeGreen,
		// Token: 0x0400004F RID: 79
		PaleGreen,
		// Token: 0x04000050 RID: 80
		Linen,
		// Token: 0x04000051 RID: 81
		Maroon,
		// Token: 0x04000052 RID: 82
		MediumAquamarine,
		// Token: 0x04000053 RID: 83
		MediumBlue,
		// Token: 0x04000054 RID: 84
		MediumOrchid,
		// Token: 0x04000055 RID: 85
		MediumPurple,
		// Token: 0x04000056 RID: 86
		MediumSeaGreen,
		// Token: 0x04000057 RID: 87
		Magenta,
		// Token: 0x04000058 RID: 88
		PaleTurquoise,
		// Token: 0x04000059 RID: 89
		PaleVioletRed,
		// Token: 0x0400005A RID: 90
		PapayaWhip,
		// Token: 0x0400005B RID: 91
		SlateGray,
		// Token: 0x0400005C RID: 92
		Snow,
		// Token: 0x0400005D RID: 93
		SpringGreen,
		// Token: 0x0400005E RID: 94
		SteelBlue,
		// Token: 0x0400005F RID: 95
		Tan,
		// Token: 0x04000060 RID: 96
		Teal,
		// Token: 0x04000061 RID: 97
		SlateBlue,
		// Token: 0x04000062 RID: 98
		Thistle,
		// Token: 0x04000063 RID: 99
		Transparent,
		// Token: 0x04000064 RID: 100
		Turquoise,
		// Token: 0x04000065 RID: 101
		Violet,
		// Token: 0x04000066 RID: 102
		Wheat,
		// Token: 0x04000067 RID: 103
		White,
		// Token: 0x04000068 RID: 104
		WhiteSmoke,
		// Token: 0x04000069 RID: 105
		Tomato,
		// Token: 0x0400006A RID: 106
		LightSeaGreen,
		// Token: 0x0400006B RID: 107
		SkyBlue,
		// Token: 0x0400006C RID: 108
		Sienna,
		// Token: 0x0400006D RID: 109
		PeachPuff,
		// Token: 0x0400006E RID: 110
		Peru,
		// Token: 0x0400006F RID: 111
		Pink,
		// Token: 0x04000070 RID: 112
		Plum,
		// Token: 0x04000071 RID: 113
		PowderBlue,
		// Token: 0x04000072 RID: 114
		Purple,
		// Token: 0x04000073 RID: 115
		Silver,
		// Token: 0x04000074 RID: 116
		Red,
		// Token: 0x04000075 RID: 117
		RoyalBlue,
		// Token: 0x04000076 RID: 118
		SaddleBrown,
		// Token: 0x04000077 RID: 119
		Salmon,
		// Token: 0x04000078 RID: 120
		SandyBrown,
		// Token: 0x04000079 RID: 121
		SeaGreen,
		// Token: 0x0400007A RID: 122
		SeaShell,
		// Token: 0x0400007B RID: 123
		RosyBrown,
		// Token: 0x0400007C RID: 124
		Yellow,
		// Token: 0x0400007D RID: 125
		LightSalmon,
		// Token: 0x0400007E RID: 126
		LightGreen,
		// Token: 0x0400007F RID: 127
		DarkRed,
		// Token: 0x04000080 RID: 128
		DarkOrchid,
		// Token: 0x04000081 RID: 129
		DarkOrange,
		// Token: 0x04000082 RID: 130
		DarkOliveGreen,
		// Token: 0x04000083 RID: 131
		DarkMagenta,
		// Token: 0x04000084 RID: 132
		DarkKhaki,
		// Token: 0x04000085 RID: 133
		DarkGreen,
		// Token: 0x04000086 RID: 134
		DarkGray,
		// Token: 0x04000087 RID: 135
		DarkGoldenrod,
		// Token: 0x04000088 RID: 136
		DarkCyan,
		// Token: 0x04000089 RID: 137
		DarkBlue,
		// Token: 0x0400008A RID: 138
		Cyan,
		// Token: 0x0400008B RID: 139
		Crimson,
		// Token: 0x0400008C RID: 140
		Cornsilk,
		// Token: 0x0400008D RID: 141
		CornflowerBlue,
		// Token: 0x0400008E RID: 142
		Coral,
		// Token: 0x0400008F RID: 143
		Chocolate,
		// Token: 0x04000090 RID: 144
		AntiqueWhite,
		// Token: 0x04000091 RID: 145
		Aqua,
		// Token: 0x04000092 RID: 146
		Aquamarine,
		// Token: 0x04000093 RID: 147
		Azure,
		// Token: 0x04000094 RID: 148
		Beige,
		// Token: 0x04000095 RID: 149
		Bisque,
		// Token: 0x04000096 RID: 150
		DarkSalmon,
		// Token: 0x04000097 RID: 151
		Black,
		// Token: 0x04000098 RID: 152
		Blue,
		// Token: 0x04000099 RID: 153
		BlueViolet,
		// Token: 0x0400009A RID: 154
		Brown,
		// Token: 0x0400009B RID: 155
		BurlyWood,
		// Token: 0x0400009C RID: 156
		CadetBlue,
		// Token: 0x0400009D RID: 157
		Chartreuse,
		// Token: 0x0400009E RID: 158
		BlanchedAlmond,
		// Token: 0x0400009F RID: 159
		DarkSeaGreen,
		// Token: 0x040000A0 RID: 160
		DarkSlateBlue,
		// Token: 0x040000A1 RID: 161
		DarkSlateGray,
		// Token: 0x040000A2 RID: 162
		HotPink,
		// Token: 0x040000A3 RID: 163
		IndianRed,
		// Token: 0x040000A4 RID: 164
		Indigo,
		// Token: 0x040000A5 RID: 165
		Ivory,
		// Token: 0x040000A6 RID: 166
		Khaki,
		// Token: 0x040000A7 RID: 167
		Lavender,
		// Token: 0x040000A8 RID: 168
		Honeydew,
		// Token: 0x040000A9 RID: 169
		LavenderBlush,
		// Token: 0x040000AA RID: 170
		LemonChiffon,
		// Token: 0x040000AB RID: 171
		LightBlue,
		// Token: 0x040000AC RID: 172
		LightCoral,
		// Token: 0x040000AD RID: 173
		LightCyan,
		// Token: 0x040000AE RID: 174
		LightGoldenrodYellow,
		// Token: 0x040000AF RID: 175
		LightGray,
		// Token: 0x040000B0 RID: 176
		LawnGreen,
		// Token: 0x040000B1 RID: 177
		LightPink,
		// Token: 0x040000B2 RID: 178
		GreenYellow,
		// Token: 0x040000B3 RID: 179
		Gray,
		// Token: 0x040000B4 RID: 180
		DarkTurquoise,
		// Token: 0x040000B5 RID: 181
		DarkViolet,
		// Token: 0x040000B6 RID: 182
		DeepPink,
		// Token: 0x040000B7 RID: 183
		DeepSkyBlue,
		// Token: 0x040000B8 RID: 184
		DimGray,
		// Token: 0x040000B9 RID: 185
		DodgerBlue,
		// Token: 0x040000BA RID: 186
		Green,
		// Token: 0x040000BB RID: 187
		Firebrick,
		// Token: 0x040000BC RID: 188
		ForestGreen,
		// Token: 0x040000BD RID: 189
		Fuchsia,
		// Token: 0x040000BE RID: 190
		Gainsboro,
		// Token: 0x040000BF RID: 191
		GhostWhite,
		// Token: 0x040000C0 RID: 192
		Gold,
		// Token: 0x040000C1 RID: 193
		Goldenrod,
		// Token: 0x040000C2 RID: 194
		FloralWhite,
		// Token: 0x040000C3 RID: 195
		YellowGreen
	}
}
