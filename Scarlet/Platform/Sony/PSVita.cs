using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Scarlet.IO;
using Scarlet.Drawing;

namespace Scarlet.Platform.Sony
{
    // https://github.com/vitasdk/vita-headers/blob/master/include/psp2/gxm.h

    public enum SceGxmTextureBaseFormat : uint
    {
        U8 = 0x00000000,
        S8 = 0x01000000,
        U4U4U4U4 = 0x02000000,
        U8U3U3U2 = 0x03000000,
        U1U5U5U5 = 0x04000000,
        U5U6U5 = 0x05000000,
        S5S5U6 = 0x06000000,
        U8U8 = 0x07000000,
        S8S8 = 0x08000000,
        U16 = 0x09000000,
        S16 = 0x0A000000,
        F16 = 0x0B000000,
        U8U8U8U8 = 0x0C000000,
        S8S8S8S8 = 0x0D000000,
        U2U10U10U10 = 0x0E000000,
        U16U16 = 0x0F000000,
        S16S16 = 0x10000000,
        F16F16 = 0x11000000,
        F32 = 0x12000000,
        F32M = 0x13000000,
        X8S8S8U8 = 0x14000000,
        X8U24 = 0x15000000,
        U32 = 0x17000000,
        S32 = 0x18000000,
        SE5M9M9M9 = 0x19000000,
        F11F11F10 = 0x1A000000,
        F16F16F16F16 = 0x1B000000,
        U16U16U16U16 = 0x1C000000,
        S16S16S16S16 = 0x1D000000,
        F32F32 = 0x1E000000,
        U32U32 = 0x1F000000,
        PVRT2BPP = 0x80000000,
        PVRT4BPP = 0x81000000,
        PVRTII2BPP = 0x82000000,
        PVRTII4BPP = 0x83000000,
        UBC1 = 0x85000000,
        UBC2 = 0x86000000,
        UBC3 = 0x87000000,
        YUV420P2 = 0x90000000,
        YUV420P3 = 0x91000000,
        YUV422 = 0x92000000,
        P4 = 0x94000000,
        P8 = 0x95000000,
        U8U8U8 = 0x98000000,
        S8S8S8 = 0x99000000,
        U2F10F10F10 = 0x9A000000
    };

    public enum SceGxmTextureSwizzle4Mode : ushort
    {
        ABGR = 0x0000,
        ARGB = 0x1000,
        RGBA = 0x2000,
        BGRA = 0x3000,
        _1BGR = 0x4000,
        _1RGB = 0x5000,
        RGB1 = 0x6000,
        BGR1 = 0x7000
    };

    public enum SceGxmTextureSwizzle3Mode : ushort
    {
        BGR = 0x0000,
        RGB = 0x1000
    };

    public enum SceGxmTextureSwizzle2Mode : ushort
    {
        GR = 0x0000,
        _00GR = 0x1000,
        GRRR = 0x2000,
        RGGG = 0x3000,
        GRGR = 0x4000,
        _00RG = 0x5000
    };

    public enum SceGxmTextureSwizzle2ModeAlt : ushort
    {
        SD = 0x0000,
        DS = 0x1000
    };

    public enum SceGxmTextureSwizzle1Mode : ushort
    {
        R = 0x0000,
        _000R = 0x1000,
        _111R = 0x2000,
        RRRR = 0x3000,
        _0RRR = 0x4000,
        _1RRR = 0x5000,
        R000 = 0x6000,
        R111 = 0x7000
    };

    public enum SceGxmTextureSwizzleYUV422Mode : ushort
    {
        YUYV_CSC0 = 0x0000,
        YVYU_CSC0 = 0x1000,
        UYVY_CSC0 = 0x2000,
        VYUY_CSC0 = 0x3000,
        YUYV_CSC1 = 0x4000,
        YVYU_CSC1 = 0x5000,
        UYVY_CSC1 = 0x6000,
        VYUY_CSC1 = 0x7000
    };

    public enum SceGxmTextureSwizzleYUV420Mode : ushort
    {
        YUV_CSC0 = 0x0000,
        YVU_CSC0 = 0x1000,
        YUV_CSC1 = 0x2000,
        YVU_CSC1 = 0x3000
    };

    public enum SceGxmTextureFormat : uint
    {
        U8_000R = SceGxmTextureBaseFormat.U8 | SceGxmTextureSwizzle1Mode._000R,
        U8_111R = SceGxmTextureBaseFormat.U8 | SceGxmTextureSwizzle1Mode._111R,
        U8_RRRR = SceGxmTextureBaseFormat.U8 | SceGxmTextureSwizzle1Mode.RRRR,
        U8_0RRR = SceGxmTextureBaseFormat.U8 | SceGxmTextureSwizzle1Mode._0RRR,
        U8_1RRR = SceGxmTextureBaseFormat.U8 | SceGxmTextureSwizzle1Mode._1RRR,
        U8_R000 = SceGxmTextureBaseFormat.U8 | SceGxmTextureSwizzle1Mode.R000,
        U8_R111 = SceGxmTextureBaseFormat.U8 | SceGxmTextureSwizzle1Mode.R111,
        U8_R = SceGxmTextureBaseFormat.U8 | SceGxmTextureSwizzle1Mode.R,
        S8_000R = SceGxmTextureBaseFormat.S8 | SceGxmTextureSwizzle1Mode._000R,
        S8_111R = SceGxmTextureBaseFormat.S8 | SceGxmTextureSwizzle1Mode._111R,
        S8_RRRR = SceGxmTextureBaseFormat.S8 | SceGxmTextureSwizzle1Mode.RRRR,
        S8_0RRR = SceGxmTextureBaseFormat.S8 | SceGxmTextureSwizzle1Mode._0RRR,
        S8_1RRR = SceGxmTextureBaseFormat.S8 | SceGxmTextureSwizzle1Mode._1RRR,
        S8_R000 = SceGxmTextureBaseFormat.S8 | SceGxmTextureSwizzle1Mode.R000,
        S8_R111 = SceGxmTextureBaseFormat.S8 | SceGxmTextureSwizzle1Mode.R111,
        S8_R = SceGxmTextureBaseFormat.S8 | SceGxmTextureSwizzle1Mode.R,
        U4U4U4U4_ABGR = SceGxmTextureBaseFormat.U4U4U4U4 | SceGxmTextureSwizzle4Mode.ABGR,
        U4U4U4U4_ARGB = SceGxmTextureBaseFormat.U4U4U4U4 | SceGxmTextureSwizzle4Mode.ARGB,
        U4U4U4U4_RGBA = SceGxmTextureBaseFormat.U4U4U4U4 | SceGxmTextureSwizzle4Mode.RGBA,
        U4U4U4U4_BGRA = SceGxmTextureBaseFormat.U4U4U4U4 | SceGxmTextureSwizzle4Mode.BGRA,
        X4U4U4U4_1BGR = SceGxmTextureBaseFormat.U4U4U4U4 | SceGxmTextureSwizzle4Mode._1BGR,
        X4U4U4U4_1RGB = SceGxmTextureBaseFormat.U4U4U4U4 | SceGxmTextureSwizzle4Mode._1RGB,
        U4U4U4X4_RGB1 = SceGxmTextureBaseFormat.U4U4U4U4 | SceGxmTextureSwizzle4Mode.RGB1,
        U4U4U4X4_BGR1 = SceGxmTextureBaseFormat.U4U4U4U4 | SceGxmTextureSwizzle4Mode.BGR1,
        U8U3U3U2_ARGB = SceGxmTextureBaseFormat.U8U3U3U2,
        U1U5U5U5_ABGR = SceGxmTextureBaseFormat.U1U5U5U5 | SceGxmTextureSwizzle4Mode.ABGR,
        U1U5U5U5_ARGB = SceGxmTextureBaseFormat.U1U5U5U5 | SceGxmTextureSwizzle4Mode.ARGB,
        U5U5U5U1_RGBA = SceGxmTextureBaseFormat.U1U5U5U5 | SceGxmTextureSwizzle4Mode.RGBA,
        U5U5U5U1_BGRA = SceGxmTextureBaseFormat.U1U5U5U5 | SceGxmTextureSwizzle4Mode.BGRA,
        X1U5U5U5_1BGR = SceGxmTextureBaseFormat.U1U5U5U5 | SceGxmTextureSwizzle4Mode._1BGR,
        X1U5U5U5_1RGB = SceGxmTextureBaseFormat.U1U5U5U5 | SceGxmTextureSwizzle4Mode._1RGB,
        U5U5U5X1_RGB1 = SceGxmTextureBaseFormat.U1U5U5U5 | SceGxmTextureSwizzle4Mode.RGB1,
        U5U5U5X1_BGR1 = SceGxmTextureBaseFormat.U1U5U5U5 | SceGxmTextureSwizzle4Mode.BGR1,
        U5U6U5_BGR = SceGxmTextureBaseFormat.U5U6U5 | SceGxmTextureSwizzle3Mode.BGR,
        U5U6U5_RGB = SceGxmTextureBaseFormat.U5U6U5 | SceGxmTextureSwizzle3Mode.RGB,
        U6S5S5_BGR = SceGxmTextureBaseFormat.S5S5U6 | SceGxmTextureSwizzle3Mode.BGR,
        S5S5U6_RGB = SceGxmTextureBaseFormat.S5S5U6 | SceGxmTextureSwizzle3Mode.RGB,
        U8U8_00GR = SceGxmTextureBaseFormat.U8U8 | SceGxmTextureSwizzle2Mode._00GR,
        U8U8_GRRR = SceGxmTextureBaseFormat.U8U8 | SceGxmTextureSwizzle2Mode.GRRR,
        U8U8_RGGG = SceGxmTextureBaseFormat.U8U8 | SceGxmTextureSwizzle2Mode.RGGG,
        U8U8_GRGR = SceGxmTextureBaseFormat.U8U8 | SceGxmTextureSwizzle2Mode.GRGR,
        U8U8_00RG = SceGxmTextureBaseFormat.U8U8 | SceGxmTextureSwizzle2Mode._00RG,
        U8U8_GR = SceGxmTextureBaseFormat.U8U8 | SceGxmTextureSwizzle2Mode.GR,
        S8S8_00GR = SceGxmTextureBaseFormat.S8S8 | SceGxmTextureSwizzle2Mode._00GR,
        S8S8_GRRR = SceGxmTextureBaseFormat.S8S8 | SceGxmTextureSwizzle2Mode.GRRR,
        S8S8_RGGG = SceGxmTextureBaseFormat.S8S8 | SceGxmTextureSwizzle2Mode.RGGG,
        S8S8_GRGR = SceGxmTextureBaseFormat.S8S8 | SceGxmTextureSwizzle2Mode.GRGR,
        S8S8_00RG = SceGxmTextureBaseFormat.S8S8 | SceGxmTextureSwizzle2Mode._00RG,
        S8S8_GR = SceGxmTextureBaseFormat.S8S8 | SceGxmTextureSwizzle2Mode.GR,
        U16_000R = SceGxmTextureBaseFormat.U16 | SceGxmTextureSwizzle1Mode._000R,
        U16_111R = SceGxmTextureBaseFormat.U16 | SceGxmTextureSwizzle1Mode._111R,
        U16_RRRR = SceGxmTextureBaseFormat.U16 | SceGxmTextureSwizzle1Mode.RRRR,
        U16_0RRR = SceGxmTextureBaseFormat.U16 | SceGxmTextureSwizzle1Mode._0RRR,
        U16_1RRR = SceGxmTextureBaseFormat.U16 | SceGxmTextureSwizzle1Mode._1RRR,
        U16_R000 = SceGxmTextureBaseFormat.U16 | SceGxmTextureSwizzle1Mode.R000,
        U16_R111 = SceGxmTextureBaseFormat.U16 | SceGxmTextureSwizzle1Mode.R111,
        U16_R = SceGxmTextureBaseFormat.U16 | SceGxmTextureSwizzle1Mode.R,
        S16_000R = SceGxmTextureBaseFormat.S16 | SceGxmTextureSwizzle1Mode._000R,
        S16_111R = SceGxmTextureBaseFormat.S16 | SceGxmTextureSwizzle1Mode._111R,
        S16_RRRR = SceGxmTextureBaseFormat.S16 | SceGxmTextureSwizzle1Mode.RRRR,
        S16_0RRR = SceGxmTextureBaseFormat.S16 | SceGxmTextureSwizzle1Mode._0RRR,
        S16_1RRR = SceGxmTextureBaseFormat.S16 | SceGxmTextureSwizzle1Mode._1RRR,
        S16_R000 = SceGxmTextureBaseFormat.S16 | SceGxmTextureSwizzle1Mode.R000,
        S16_R111 = SceGxmTextureBaseFormat.S16 | SceGxmTextureSwizzle1Mode.R111,
        S16_R = SceGxmTextureBaseFormat.S16 | SceGxmTextureSwizzle1Mode.R,
        F16_000R = SceGxmTextureBaseFormat.F16 | SceGxmTextureSwizzle1Mode._000R,
        F16_111R = SceGxmTextureBaseFormat.F16 | SceGxmTextureSwizzle1Mode._111R,
        F16_RRRR = SceGxmTextureBaseFormat.F16 | SceGxmTextureSwizzle1Mode.RRRR,
        F16_0RRR = SceGxmTextureBaseFormat.F16 | SceGxmTextureSwizzle1Mode._0RRR,
        F16_1RRR = SceGxmTextureBaseFormat.F16 | SceGxmTextureSwizzle1Mode._1RRR,
        F16_R000 = SceGxmTextureBaseFormat.F16 | SceGxmTextureSwizzle1Mode.R000,
        F16_R111 = SceGxmTextureBaseFormat.F16 | SceGxmTextureSwizzle1Mode.R111,
        F16_R = SceGxmTextureBaseFormat.F16 | SceGxmTextureSwizzle1Mode.R,
        U8U8U8U8_ABGR = SceGxmTextureBaseFormat.U8U8U8U8 | SceGxmTextureSwizzle4Mode.ABGR,
        U8U8U8U8_ARGB = SceGxmTextureBaseFormat.U8U8U8U8 | SceGxmTextureSwizzle4Mode.ARGB,
        U8U8U8U8_RGBA = SceGxmTextureBaseFormat.U8U8U8U8 | SceGxmTextureSwizzle4Mode.RGBA,
        U8U8U8U8_BGRA = SceGxmTextureBaseFormat.U8U8U8U8 | SceGxmTextureSwizzle4Mode.BGRA,
        X8U8U8U8_1BGR = SceGxmTextureBaseFormat.U8U8U8U8 | SceGxmTextureSwizzle4Mode._1BGR,
        X8U8U8U8_1RGB = SceGxmTextureBaseFormat.U8U8U8U8 | SceGxmTextureSwizzle4Mode._1RGB,
        U8U8U8X8_RGB1 = SceGxmTextureBaseFormat.U8U8U8U8 | SceGxmTextureSwizzle4Mode.RGB1,
        U8U8U8X8_BGR1 = SceGxmTextureBaseFormat.U8U8U8U8 | SceGxmTextureSwizzle4Mode.BGR1,
        S8S8S8S8_ABGR = SceGxmTextureBaseFormat.S8S8S8S8 | SceGxmTextureSwizzle4Mode.ABGR,
        S8S8S8S8_ARGB = SceGxmTextureBaseFormat.S8S8S8S8 | SceGxmTextureSwizzle4Mode.ARGB,
        S8S8S8S8_RGBA = SceGxmTextureBaseFormat.S8S8S8S8 | SceGxmTextureSwizzle4Mode.RGBA,
        S8S8S8S8_BGRA = SceGxmTextureBaseFormat.S8S8S8S8 | SceGxmTextureSwizzle4Mode.BGRA,
        X8S8S8S8_1BGR = SceGxmTextureBaseFormat.S8S8S8S8 | SceGxmTextureSwizzle4Mode._1BGR,
        X8S8S8S8_1RGB = SceGxmTextureBaseFormat.S8S8S8S8 | SceGxmTextureSwizzle4Mode._1RGB,
        S8S8S8X8_RGB1 = SceGxmTextureBaseFormat.S8S8S8S8 | SceGxmTextureSwizzle4Mode.RGB1,
        S8S8S8X8_BGR1 = SceGxmTextureBaseFormat.S8S8S8S8 | SceGxmTextureSwizzle4Mode.BGR1,
        U2U10U10U10_ABGR = SceGxmTextureBaseFormat.U2U10U10U10 | SceGxmTextureSwizzle4Mode.ABGR,
        U2U10U10U10_ARGB = SceGxmTextureBaseFormat.U2U10U10U10 | SceGxmTextureSwizzle4Mode.ARGB,
        U10U10U10U2_RGBA = SceGxmTextureBaseFormat.U2U10U10U10 | SceGxmTextureSwizzle4Mode.RGBA,
        U10U10U10U2_BGRA = SceGxmTextureBaseFormat.U2U10U10U10 | SceGxmTextureSwizzle4Mode.BGRA,
        X2U10U10U10_1BGR = SceGxmTextureBaseFormat.U2U10U10U10 | SceGxmTextureSwizzle4Mode._1BGR,
        X2U10U10U10_1RGB = SceGxmTextureBaseFormat.U2U10U10U10 | SceGxmTextureSwizzle4Mode._1RGB,
        U10U10U10X2_RGB1 = SceGxmTextureBaseFormat.U2U10U10U10 | SceGxmTextureSwizzle4Mode.RGB1,
        U10U10U10X2_BGR1 = SceGxmTextureBaseFormat.U2U10U10U10 | SceGxmTextureSwizzle4Mode.BGR1,
        U16U16_00GR = SceGxmTextureBaseFormat.U16U16 | SceGxmTextureSwizzle2Mode._00GR,
        U16U16_GRRR = SceGxmTextureBaseFormat.U16U16 | SceGxmTextureSwizzle2Mode.GRRR,
        U16U16_RGGG = SceGxmTextureBaseFormat.U16U16 | SceGxmTextureSwizzle2Mode.RGGG,
        U16U16_GRGR = SceGxmTextureBaseFormat.U16U16 | SceGxmTextureSwizzle2Mode.GRGR,
        U16U16_00RG = SceGxmTextureBaseFormat.U16U16 | SceGxmTextureSwizzle2Mode._00RG,
        U16U16_GR = SceGxmTextureBaseFormat.U16U16 | SceGxmTextureSwizzle2Mode.GR,
        S16S16_00GR = SceGxmTextureBaseFormat.S16S16 | SceGxmTextureSwizzle2Mode._00GR,
        S16S16_GRRR = SceGxmTextureBaseFormat.S16S16 | SceGxmTextureSwizzle2Mode.GRRR,
        S16S16_RGGG = SceGxmTextureBaseFormat.S16S16 | SceGxmTextureSwizzle2Mode.RGGG,
        S16S16_GRGR = SceGxmTextureBaseFormat.S16S16 | SceGxmTextureSwizzle2Mode.GRGR,
        S16S16_00RG = SceGxmTextureBaseFormat.S16S16 | SceGxmTextureSwizzle2Mode._00RG,
        S16S16_GR = SceGxmTextureBaseFormat.S16S16 | SceGxmTextureSwizzle2Mode.GR,
        F16F16_00GR = SceGxmTextureBaseFormat.F16F16 | SceGxmTextureSwizzle2Mode._00GR,
        F16F16_GRRR = SceGxmTextureBaseFormat.F16F16 | SceGxmTextureSwizzle2Mode.GRRR,
        F16F16_RGGG = SceGxmTextureBaseFormat.F16F16 | SceGxmTextureSwizzle2Mode.RGGG,
        F16F16_GRGR = SceGxmTextureBaseFormat.F16F16 | SceGxmTextureSwizzle2Mode.GRGR,
        F16F16_00RG = SceGxmTextureBaseFormat.F16F16 | SceGxmTextureSwizzle2Mode._00RG,
        F16F16_GR = SceGxmTextureBaseFormat.F16F16 | SceGxmTextureSwizzle2Mode.GR,
        F32_000R = SceGxmTextureBaseFormat.F32 | SceGxmTextureSwizzle1Mode._000R,
        F32_111R = SceGxmTextureBaseFormat.F32 | SceGxmTextureSwizzle1Mode._111R,
        F32_RRRR = SceGxmTextureBaseFormat.F32 | SceGxmTextureSwizzle1Mode.RRRR,
        F32_0RRR = SceGxmTextureBaseFormat.F32 | SceGxmTextureSwizzle1Mode._0RRR,
        F32_1RRR = SceGxmTextureBaseFormat.F32 | SceGxmTextureSwizzle1Mode._1RRR,
        F32_R000 = SceGxmTextureBaseFormat.F32 | SceGxmTextureSwizzle1Mode.R000,
        F32_R111 = SceGxmTextureBaseFormat.F32 | SceGxmTextureSwizzle1Mode.R111,
        F32_R = SceGxmTextureBaseFormat.F32 | SceGxmTextureSwizzle1Mode.R,
        F32M_000R = SceGxmTextureBaseFormat.F32M | SceGxmTextureSwizzle1Mode._000R,
        F32M_111R = SceGxmTextureBaseFormat.F32M | SceGxmTextureSwizzle1Mode._111R,
        F32M_RRRR = SceGxmTextureBaseFormat.F32M | SceGxmTextureSwizzle1Mode.RRRR,
        F32M_0RRR = SceGxmTextureBaseFormat.F32M | SceGxmTextureSwizzle1Mode._0RRR,
        F32M_1RRR = SceGxmTextureBaseFormat.F32M | SceGxmTextureSwizzle1Mode._1RRR,
        F32M_R000 = SceGxmTextureBaseFormat.F32M | SceGxmTextureSwizzle1Mode.R000,
        F32M_R111 = SceGxmTextureBaseFormat.F32M | SceGxmTextureSwizzle1Mode.R111,
        F32M_R = SceGxmTextureBaseFormat.F32M | SceGxmTextureSwizzle1Mode.R,
        X8S8S8U8_1BGR = SceGxmTextureBaseFormat.X8S8S8U8 | SceGxmTextureSwizzle3Mode.BGR,
        X8U8S8S8_1RGB = SceGxmTextureBaseFormat.X8S8S8U8 | SceGxmTextureSwizzle3Mode.RGB,
        X8U24_SD = SceGxmTextureBaseFormat.X8U24 | SceGxmTextureSwizzle2ModeAlt.SD,
        U24X8_DS = SceGxmTextureBaseFormat.X8U24 | SceGxmTextureSwizzle2ModeAlt.DS,
        U32_000R = SceGxmTextureBaseFormat.U32 | SceGxmTextureSwizzle1Mode._000R,
        U32_111R = SceGxmTextureBaseFormat.U32 | SceGxmTextureSwizzle1Mode._111R,
        U32_RRRR = SceGxmTextureBaseFormat.U32 | SceGxmTextureSwizzle1Mode.RRRR,
        U32_0RRR = SceGxmTextureBaseFormat.U32 | SceGxmTextureSwizzle1Mode._0RRR,
        U32_1RRR = SceGxmTextureBaseFormat.U32 | SceGxmTextureSwizzle1Mode._1RRR,
        U32_R000 = SceGxmTextureBaseFormat.U32 | SceGxmTextureSwizzle1Mode.R000,
        U32_R111 = SceGxmTextureBaseFormat.U32 | SceGxmTextureSwizzle1Mode.R111,
        U32_R = SceGxmTextureBaseFormat.U32 | SceGxmTextureSwizzle1Mode.R,
        S32_000R = SceGxmTextureBaseFormat.S32 | SceGxmTextureSwizzle1Mode._000R,
        S32_111R = SceGxmTextureBaseFormat.S32 | SceGxmTextureSwizzle1Mode._111R,
        S32_RRRR = SceGxmTextureBaseFormat.S32 | SceGxmTextureSwizzle1Mode.RRRR,
        S32_0RRR = SceGxmTextureBaseFormat.S32 | SceGxmTextureSwizzle1Mode._0RRR,
        S32_1RRR = SceGxmTextureBaseFormat.S32 | SceGxmTextureSwizzle1Mode._1RRR,
        S32_R000 = SceGxmTextureBaseFormat.S32 | SceGxmTextureSwizzle1Mode.R000,
        S32_R111 = SceGxmTextureBaseFormat.S32 | SceGxmTextureSwizzle1Mode.R111,
        S32_R = SceGxmTextureBaseFormat.S32 | SceGxmTextureSwizzle1Mode.R,
        SE5M9M9M9_BGR = SceGxmTextureBaseFormat.SE5M9M9M9 | SceGxmTextureSwizzle3Mode.BGR,
        SE5M9M9M9_RGB = SceGxmTextureBaseFormat.SE5M9M9M9 | SceGxmTextureSwizzle3Mode.RGB,
        F10F11F11_BGR = SceGxmTextureBaseFormat.F11F11F10 | SceGxmTextureSwizzle3Mode.BGR,
        F11F11F10_RGB = SceGxmTextureBaseFormat.F11F11F10 | SceGxmTextureSwizzle3Mode.RGB,
        F16F16F16F16_ABGR = SceGxmTextureBaseFormat.F16F16F16F16 | SceGxmTextureSwizzle4Mode.ABGR,
        F16F16F16F16_ARGB = SceGxmTextureBaseFormat.F16F16F16F16 | SceGxmTextureSwizzle4Mode.ARGB,
        F16F16F16F16_RGBA = SceGxmTextureBaseFormat.F16F16F16F16 | SceGxmTextureSwizzle4Mode.RGBA,
        F16F16F16F16_BGRA = SceGxmTextureBaseFormat.F16F16F16F16 | SceGxmTextureSwizzle4Mode.BGRA,
        X16F16F16F16_1BGR = SceGxmTextureBaseFormat.F16F16F16F16 | SceGxmTextureSwizzle4Mode._1BGR,
        X16F16F16F16_1RGB = SceGxmTextureBaseFormat.F16F16F16F16 | SceGxmTextureSwizzle4Mode._1RGB,
        F16F16F16X16_RGB1 = SceGxmTextureBaseFormat.F16F16F16F16 | SceGxmTextureSwizzle4Mode.RGB1,
        F16F16F16X16_BGR1 = SceGxmTextureBaseFormat.F16F16F16F16 | SceGxmTextureSwizzle4Mode.BGR1,
        U16U16U16U16_ABGR = SceGxmTextureBaseFormat.U16U16U16U16 | SceGxmTextureSwizzle4Mode.ABGR,
        U16U16U16U16_ARGB = SceGxmTextureBaseFormat.U16U16U16U16 | SceGxmTextureSwizzle4Mode.ARGB,
        U16U16U16U16_RGBA = SceGxmTextureBaseFormat.U16U16U16U16 | SceGxmTextureSwizzle4Mode.RGBA,
        U16U16U16U16_BGRA = SceGxmTextureBaseFormat.U16U16U16U16 | SceGxmTextureSwizzle4Mode.BGRA,
        X16U16U16U16_1BGR = SceGxmTextureBaseFormat.U16U16U16U16 | SceGxmTextureSwizzle4Mode._1BGR,
        X16U16U16U16_1RGB = SceGxmTextureBaseFormat.U16U16U16U16 | SceGxmTextureSwizzle4Mode._1RGB,
        U16U16U16X16_RGB1 = SceGxmTextureBaseFormat.U16U16U16U16 | SceGxmTextureSwizzle4Mode.RGB1,
        U16U16U16X16_BGR1 = SceGxmTextureBaseFormat.U16U16U16U16 | SceGxmTextureSwizzle4Mode.BGR1,
        S16S16S16S16_ABGR = SceGxmTextureBaseFormat.S16S16S16S16 | SceGxmTextureSwizzle4Mode.ABGR,
        S16S16S16S16_ARGB = SceGxmTextureBaseFormat.S16S16S16S16 | SceGxmTextureSwizzle4Mode.ARGB,
        S16S16S16S16_RGBA = SceGxmTextureBaseFormat.S16S16S16S16 | SceGxmTextureSwizzle4Mode.RGBA,
        S16S16S16S16_BGRA = SceGxmTextureBaseFormat.S16S16S16S16 | SceGxmTextureSwizzle4Mode.BGRA,
        X16S16S16S16_1BGR = SceGxmTextureBaseFormat.S16S16S16S16 | SceGxmTextureSwizzle4Mode._1BGR,
        X16S16S16S16_1RGB = SceGxmTextureBaseFormat.S16S16S16S16 | SceGxmTextureSwizzle4Mode._1RGB,
        S16S16S16X16_RGB1 = SceGxmTextureBaseFormat.S16S16S16S16 | SceGxmTextureSwizzle4Mode.RGB1,
        S16S16S16X16_BGR1 = SceGxmTextureBaseFormat.S16S16S16S16 | SceGxmTextureSwizzle4Mode.BGR1,
        F32F32_00GR = SceGxmTextureBaseFormat.F32F32 | SceGxmTextureSwizzle2Mode._00GR,
        F32F32_GRRR = SceGxmTextureBaseFormat.F32F32 | SceGxmTextureSwizzle2Mode.GRRR,
        F32F32_RGGG = SceGxmTextureBaseFormat.F32F32 | SceGxmTextureSwizzle2Mode.RGGG,
        F32F32_GRGR = SceGxmTextureBaseFormat.F32F32 | SceGxmTextureSwizzle2Mode.GRGR,
        F32F32_00RG = SceGxmTextureBaseFormat.F32F32 | SceGxmTextureSwizzle2Mode._00RG,
        F32F32_GR = SceGxmTextureBaseFormat.F32F32 | SceGxmTextureSwizzle2Mode.GR,
        U32U32_00GR = SceGxmTextureBaseFormat.U32U32 | SceGxmTextureSwizzle2Mode._00GR,
        U32U32_GRRR = SceGxmTextureBaseFormat.U32U32 | SceGxmTextureSwizzle2Mode.GRRR,
        U32U32_RGGG = SceGxmTextureBaseFormat.U32U32 | SceGxmTextureSwizzle2Mode.RGGG,
        U32U32_GRGR = SceGxmTextureBaseFormat.U32U32 | SceGxmTextureSwizzle2Mode.GRGR,
        U32U32_00RG = SceGxmTextureBaseFormat.U32U32 | SceGxmTextureSwizzle2Mode._00RG,
        U32U32_GR = SceGxmTextureBaseFormat.U32U32 | SceGxmTextureSwizzle2Mode.GR,
        PVRT2BPP_ABGR = SceGxmTextureBaseFormat.PVRT2BPP | SceGxmTextureSwizzle4Mode.ABGR,
        PVRT2BPP_1BGR = SceGxmTextureBaseFormat.PVRT2BPP | SceGxmTextureSwizzle4Mode._1BGR,
        PVRT4BPP_ABGR = SceGxmTextureBaseFormat.PVRT4BPP | SceGxmTextureSwizzle4Mode.ABGR,
        PVRT4BPP_1BGR = SceGxmTextureBaseFormat.PVRT4BPP | SceGxmTextureSwizzle4Mode._1BGR,
        PVRTII2BPP_ABGR = SceGxmTextureBaseFormat.PVRTII2BPP | SceGxmTextureSwizzle4Mode.ABGR,
        PVRTII2BPP_1BGR = SceGxmTextureBaseFormat.PVRTII2BPP | SceGxmTextureSwizzle4Mode._1BGR,
        PVRTII4BPP_ABGR = SceGxmTextureBaseFormat.PVRTII4BPP | SceGxmTextureSwizzle4Mode.ABGR,
        PVRTII4BPP_1BGR = SceGxmTextureBaseFormat.PVRTII4BPP | SceGxmTextureSwizzle4Mode._1BGR,
        UBC1_ABGR = SceGxmTextureBaseFormat.UBC1 | SceGxmTextureSwizzle4Mode.ABGR,
        UBC2_ABGR = SceGxmTextureBaseFormat.UBC2 | SceGxmTextureSwizzle4Mode.ABGR,
        UBC3_ABGR = SceGxmTextureBaseFormat.UBC3 | SceGxmTextureSwizzle4Mode.ABGR,
        YUV420P2_CSC0 = SceGxmTextureBaseFormat.YUV420P2 | SceGxmTextureSwizzleYUV420Mode.YUV_CSC0,
        YVU420P2_CSC0 = SceGxmTextureBaseFormat.YUV420P2 | SceGxmTextureSwizzleYUV420Mode.YVU_CSC0,
        YUV420P2_CSC1 = SceGxmTextureBaseFormat.YUV420P2 | SceGxmTextureSwizzleYUV420Mode.YUV_CSC1,
        YVU420P2_CSC1 = SceGxmTextureBaseFormat.YUV420P2 | SceGxmTextureSwizzleYUV420Mode.YVU_CSC1,
        YUV420P3_CSC0 = SceGxmTextureBaseFormat.YUV420P3 | SceGxmTextureSwizzleYUV420Mode.YUV_CSC0,
        YVU420P3_CSC0 = SceGxmTextureBaseFormat.YUV420P3 | SceGxmTextureSwizzleYUV420Mode.YVU_CSC0,
        YUV420P3_CSC1 = SceGxmTextureBaseFormat.YUV420P3 | SceGxmTextureSwizzleYUV420Mode.YUV_CSC1,
        YVU420P3_CSC1 = SceGxmTextureBaseFormat.YUV420P3 | SceGxmTextureSwizzleYUV420Mode.YVU_CSC1,
        YUYV422_CSC0 = SceGxmTextureBaseFormat.YUV422 | SceGxmTextureSwizzleYUV422Mode.YUYV_CSC0,
        YVYU422_CSC0 = SceGxmTextureBaseFormat.YUV422 | SceGxmTextureSwizzleYUV422Mode.YVYU_CSC0,
        UYVY422_CSC0 = SceGxmTextureBaseFormat.YUV422 | SceGxmTextureSwizzleYUV422Mode.UYVY_CSC0,
        VYUY422_CSC0 = SceGxmTextureBaseFormat.YUV422 | SceGxmTextureSwizzleYUV422Mode.VYUY_CSC0,
        YUYV422_CSC1 = SceGxmTextureBaseFormat.YUV422 | SceGxmTextureSwizzleYUV422Mode.YUYV_CSC1,
        YVYU422_CSC1 = SceGxmTextureBaseFormat.YUV422 | SceGxmTextureSwizzleYUV422Mode.YVYU_CSC1,
        UYVY422_CSC1 = SceGxmTextureBaseFormat.YUV422 | SceGxmTextureSwizzleYUV422Mode.UYVY_CSC1,
        VYUY422_CSC1 = SceGxmTextureBaseFormat.YUV422 | SceGxmTextureSwizzleYUV422Mode.VYUY_CSC1,
        P4_ABGR = SceGxmTextureBaseFormat.P4 | SceGxmTextureSwizzle4Mode.ABGR,
        P4_ARGB = SceGxmTextureBaseFormat.P4 | SceGxmTextureSwizzle4Mode.ARGB,
        P4_RGBA = SceGxmTextureBaseFormat.P4 | SceGxmTextureSwizzle4Mode.RGBA,
        P4_BGRA = SceGxmTextureBaseFormat.P4 | SceGxmTextureSwizzle4Mode.BGRA,
        P4_1BGR = SceGxmTextureBaseFormat.P4 | SceGxmTextureSwizzle4Mode._1BGR,
        P4_1RGB = SceGxmTextureBaseFormat.P4 | SceGxmTextureSwizzle4Mode._1RGB,
        P4_RGB1 = SceGxmTextureBaseFormat.P4 | SceGxmTextureSwizzle4Mode.RGB1,
        P4_BGR1 = SceGxmTextureBaseFormat.P4 | SceGxmTextureSwizzle4Mode.BGR1,
        P8_ABGR = SceGxmTextureBaseFormat.P8 | SceGxmTextureSwizzle4Mode.ABGR,
        P8_ARGB = SceGxmTextureBaseFormat.P8 | SceGxmTextureSwizzle4Mode.ARGB,
        P8_RGBA = SceGxmTextureBaseFormat.P8 | SceGxmTextureSwizzle4Mode.RGBA,
        P8_BGRA = SceGxmTextureBaseFormat.P8 | SceGxmTextureSwizzle4Mode.BGRA,
        P8_1BGR = SceGxmTextureBaseFormat.P8 | SceGxmTextureSwizzle4Mode._1BGR,
        P8_1RGB = SceGxmTextureBaseFormat.P8 | SceGxmTextureSwizzle4Mode._1RGB,
        P8_RGB1 = SceGxmTextureBaseFormat.P8 | SceGxmTextureSwizzle4Mode.RGB1,
        P8_BGR1 = SceGxmTextureBaseFormat.P8 | SceGxmTextureSwizzle4Mode.BGR1,
        U8U8U8_BGR = SceGxmTextureBaseFormat.U8U8U8 | SceGxmTextureSwizzle3Mode.BGR,
        U8U8U8_RGB = SceGxmTextureBaseFormat.U8U8U8 | SceGxmTextureSwizzle3Mode.RGB,
        S8S8S8_BGR = SceGxmTextureBaseFormat.S8S8S8 | SceGxmTextureSwizzle3Mode.BGR,
        S8S8S8_RGB = SceGxmTextureBaseFormat.S8S8S8 | SceGxmTextureSwizzle3Mode.RGB,
        U2F10F10F10_ABGR = SceGxmTextureBaseFormat.U2F10F10F10 | SceGxmTextureSwizzle4Mode.ABGR,
        U2F10F10F10_ARGB = SceGxmTextureBaseFormat.U2F10F10F10 | SceGxmTextureSwizzle4Mode.ARGB,
        F10F10F10U2_RGBA = SceGxmTextureBaseFormat.U2F10F10F10 | SceGxmTextureSwizzle4Mode.RGBA,
        F10F10F10U2_BGRA = SceGxmTextureBaseFormat.U2F10F10F10 | SceGxmTextureSwizzle4Mode.BGRA,
        X2F10F10F10_1BGR = SceGxmTextureBaseFormat.U2F10F10F10 | SceGxmTextureSwizzle4Mode._1BGR,
        X2F10F10F10_1RGB = SceGxmTextureBaseFormat.U2F10F10F10 | SceGxmTextureSwizzle4Mode._1RGB,
        F10F10F10X2_RGB1 = SceGxmTextureBaseFormat.U2F10F10F10 | SceGxmTextureSwizzle4Mode.RGB1,
        F10F10F10X2_BGR1 = SceGxmTextureBaseFormat.U2F10F10F10 | SceGxmTextureSwizzle4Mode.BGR1,

        // Legacy formats
        L8 = SceGxmTextureFormat.U8_1RRR,
        A8 = SceGxmTextureFormat.U8_R000,
        R8 = SceGxmTextureFormat.U8_000R,
        A4R4G4B4 = SceGxmTextureFormat.U4U4U4U4_ARGB,
        A1R5G5B5 = SceGxmTextureFormat.U1U5U5U5_ARGB,
        R5G6B5 = SceGxmTextureFormat.U5U6U5_RGB,
        A8L8 = SceGxmTextureFormat.U8U8_GRRR,
        L8A8 = SceGxmTextureFormat.U8U8_RGGG,
        G8R8 = SceGxmTextureFormat.U8U8_00GR,
        L16 = SceGxmTextureFormat.U16_1RRR,
        A16 = SceGxmTextureFormat.U16_R000,
        R16 = SceGxmTextureFormat.U16_000R,
        D16 = SceGxmTextureFormat.U16_R,
        LF16 = SceGxmTextureFormat.F16_1RRR,
        AF16 = SceGxmTextureFormat.F16_R000,
        RF16 = SceGxmTextureFormat.F16_000R,
        A8R8G8B8 = SceGxmTextureFormat.U8U8U8U8_ARGB,
        A8B8G8R8 = SceGxmTextureFormat.U8U8U8U8_ABGR,
        AF16LF16 = SceGxmTextureFormat.F16F16_GRRR,
        LF16AF16 = SceGxmTextureFormat.F16F16_RGGG,
        GF16RF16 = SceGxmTextureFormat.F16F16_00GR,
        LF32M = SceGxmTextureFormat.F32M_1RRR,
        AF32M = SceGxmTextureFormat.F32M_R000,
        RF32M = SceGxmTextureFormat.F32M_000R,
        DF32M = SceGxmTextureFormat.F32M_R,
        VYUY = SceGxmTextureFormat.VYUY422_CSC0,
        YVYU = SceGxmTextureFormat.YVYU422_CSC0,
        UBC1 = SceGxmTextureFormat.UBC1_ABGR,
        UBC2 = SceGxmTextureFormat.UBC2_ABGR,
        UBC3 = SceGxmTextureFormat.UBC3_ABGR,
        PVRT2BPP = SceGxmTextureFormat.PVRT2BPP_ABGR,
        PVRT4BPP = SceGxmTextureFormat.PVRT4BPP_ABGR,
        PVRTII2BPP = SceGxmTextureFormat.PVRTII2BPP_ABGR,
        PVRTII4BPP = SceGxmTextureFormat.PVRTII4BPP_ABGR
    };

    public enum SceGxmTextureType : uint
    {
        Swizzled = 0x00000000,
        Cube = 0x40000000,
        Linear = 0x60000000,
        Tiled = 0x80000000,
        LinearStrided = 0xC0000000
    };

    public class SceGxtHeader
    {
        public const string ExpectedMagicNumber = "GXT\0";

        public string MagicNumber { get; private set; }
        public uint Version { get; private set; } // TODO: 0x10000003 == 3.01 ???
        public uint NumTextures { get; private set; }
        public uint TextureDataOffset { get; private set; }
        public uint TextureDataSize { get; private set; }
        public uint NumP4Palettes { get; private set; }
        public uint NumP8Palettes { get; private set; }
        public uint Padding { get; private set; }

        public SceGxtHeader(EndianBinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
            Version = reader.ReadUInt32();
            NumTextures = reader.ReadUInt32();
            TextureDataOffset = reader.ReadUInt32();
            TextureDataSize = reader.ReadUInt32();
            NumP4Palettes = reader.ReadUInt32();
            NumP8Palettes = reader.ReadUInt32();
            Padding = reader.ReadUInt32();
        }
    }

    public abstract class SceGxtTextureInfo
    {
        public uint DataOffset { get; private set; }
        public uint DataSize { get; private set; }
        public int PaletteIndex { get; private set; }
        public uint Flags { get; private set; }
        public uint[] ControlWords { get; private set; }

        public abstract SceGxmTextureType GetTextureType();
        public abstract SceGxmTextureFormat GetTextureFormat();
        public abstract ushort GetWidth();
        public abstract ushort GetHeight();

        //TODO: where's byteStride for texture type LinearStrided?

        public SceGxmTextureBaseFormat GetTextureBaseFormat()
        {
            return (SceGxmTextureBaseFormat)((uint)GetTextureFormat() & 0xFFFF0000);
        }

        public SceGxtTextureInfo(EndianBinaryReader reader)
        {
            DataOffset = reader.ReadUInt32();
            DataSize = reader.ReadUInt32();
            PaletteIndex = reader.ReadInt32();
            Flags = reader.ReadUInt32();
            ControlWords = new uint[4];
            for (int i = 0; i < ControlWords.Length; i++) ControlWords[i] = reader.ReadUInt32();
        }

        public ushort GetWidthRounded()
        {
            int roundedWidth = 1;
            while (roundedWidth < GetWidth()) roundedWidth *= 2;
            return (ushort)roundedWidth;
        }

        public ushort GetHeightRounded()
        {
            int roundedHeight = 1;
            while (roundedHeight < GetHeight()) roundedHeight *= 2;
            return (ushort)roundedHeight;
        }
    }

    public class SceGxtTextureInfoV301 : SceGxtTextureInfo
    {
        public SceGxtTextureInfoV301(EndianBinaryReader reader) : base(reader) { }

        public override SceGxmTextureType GetTextureType() { return (SceGxmTextureType)ControlWords[0]; }
        public override SceGxmTextureFormat GetTextureFormat() { return (SceGxmTextureFormat)ControlWords[1]; }
        public override ushort GetWidth() { return (ushort)(ControlWords[2] & 0xFFFF); }
        public override ushort GetHeight() { return (ushort)(ControlWords[2] >> 16); }
    }

    // TODO: verify me! what about texture formats < 0x80000000? is texture type correct?
    public class SceGxtTextureInfoV201 : SceGxtTextureInfo
    {
        public SceGxtTextureInfoV201(EndianBinaryReader reader) : base(reader) { }

        public override SceGxmTextureType GetTextureType() { return (SceGxmTextureType)ControlWords[2]; }
        public override SceGxmTextureFormat GetTextureFormat() { return (SceGxmTextureFormat)(0x80000000 | ((ControlWords[1] >> 24) & 0xF) << 24); }
        public override ushort GetWidth() { return (ushort)(1 << (ushort)((ControlWords[1] >> 16) & 0xF)); }
        public override ushort GetHeight() { return (ushort)(1 << (ushort)((ControlWords[1] >> 0) & 0xF)); }
    }

    // TODO: verify me; same as v201?
    public class SceGxtTextureInfoV101 : SceGxtTextureInfo
    {
        public SceGxtTextureInfoV101(EndianBinaryReader reader) : base(reader) { }

        public override SceGxmTextureType GetTextureType() { return (SceGxmTextureType)ControlWords[2]; }
        public override SceGxmTextureFormat GetTextureFormat() { return (SceGxmTextureFormat)(0x80000000 | ((ControlWords[1] >> 24) & 0xF) << 24); }
        public override ushort GetWidth() { return (ushort)(1 << (ushort)((ControlWords[1] >> 16) & 0xF)); }
        public override ushort GetHeight() { return (ushort)(1 << (ushort)((ControlWords[1] >> 0) & 0xF)); }
    }

    // TODO: verify me!
    public class BUVChunk
    {
        public const string ExpectedMagicNumber = "BUV\0";

        public string MagicNumber { get; private set; }
        public uint NumEntries { get; private set; }

        public BUVEntry[] Entries { get; private set; }

        public BUVChunk(EndianBinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
            NumEntries = reader.ReadUInt32();

            Entries = new BUVEntry[NumEntries];
            for (int i = 0; i < Entries.Length; i++) Entries[i] = new BUVEntry(reader);
        }
    }

    // TODO: verify me!
    public class BUVEntry
    {
        public ushort X { get; private set; }
        public ushort Y { get; private set; }
        public ushort Width { get; private set; }
        public ushort Height { get; private set; }
        public short PaletteIndex { get; private set; }
        public ushort Unknown0x0A { get; private set; }

        public BUVEntry(EndianBinaryReader reader)
        {
            X = reader.ReadUInt16();
            Y = reader.ReadUInt16();
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            PaletteIndex = reader.ReadInt16();
            Unknown0x0A = reader.ReadUInt16();
        }
    }

    public static class PSVita
    {
        static readonly Dictionary<SceGxmTextureFormat, PixelDataFormat> formatMap = new Dictionary<SceGxmTextureFormat, PixelDataFormat>()
        {
            /* L8       */ { SceGxmTextureFormat.U8_1RRR, PixelDataFormat.FormatLuminance8 },
            /* A8 (x00) */ { SceGxmTextureFormat.U8_R000, PixelDataFormat.FormatAlpha8 },
            /* A8 (xFF) */ { SceGxmTextureFormat.U8_R111, PixelDataFormat.FormatAlpha8 | PixelDataFormat.ForceFull },
            /* LA88     */ { SceGxmTextureFormat.U8U8_RGGG, PixelDataFormat.FormatLuminanceAlpha88 },
            /* AL88     */ { SceGxmTextureFormat.U8U8_GRRR, PixelDataFormat.FormatAlphaLuminance88 },
            // RG88     */ { SceGxmTextureFormat.U8U8_00GR, PixelDataFormat.Undefined },
            /* ARGB1555 */ { SceGxmTextureFormat.U1U5U5U5_ARGB, PixelDataFormat.FormatArgb1555 },
            /* ARGB4444 */ { SceGxmTextureFormat.U4U4U4U4_ARGB, PixelDataFormat.FormatArgb4444 },
            /* RGB565   */ { SceGxmTextureFormat.U5U6U5_RGB, PixelDataFormat.FormatRgb565 },
            /* ABGR8888 */ { SceGxmTextureFormat.U8U8U8U8_ABGR, PixelDataFormat.FormatAbgr8888 },
            /* ARGB8888 */ { SceGxmTextureFormat.U8U8U8U8_ARGB, PixelDataFormat.FormatArgb8888 },
            /* XRGB8888 */ { SceGxmTextureFormat.X8U8U8U8_1RGB, PixelDataFormat.FormatXrgb8888 },
            /* DXT1     */ { SceGxmTextureFormat.UBC1_ABGR, PixelDataFormat.FormatDXT1Rgb },
            /* DXT3     */ { SceGxmTextureFormat.UBC2_ABGR, PixelDataFormat.FormatDXT3 },
            /* DXT5     */ { SceGxmTextureFormat.UBC3_ABGR, PixelDataFormat.FormatDXT5 },
            /* PVRT2    */ { SceGxmTextureFormat.PVRT2BPP_ABGR, PixelDataFormat.FormatPVRT2_Vita },
            // PVRT2    */ { SceGxmTextureFormat.PVRT2BPP_1BGR, PixelDataFormat.Undefined },
            /* PVRT4    */ { SceGxmTextureFormat.PVRT4BPP_ABGR, PixelDataFormat.FormatPVRT4_Vita },
            // PVRT4    */ { SceGxmTextureFormat.PVRT4BPP_1BGR, PixelDataFormat.Undefined },
            // PVRTII2  */ { SceGxmTextureFormat.PVRTII2BPP_ABGR, PixelDataFormat.Undefined },
            // PVRTII2  */ { SceGxmTextureFormat.PVRTII2BPP_1BGR, PixelDataFormat.Undefined },
            // PVRTII4  */ { SceGxmTextureFormat.PVRTII4BPP_ABGR, PixelDataFormat.Undefined },
            // PVRTII4  */ { SceGxmTextureFormat.PVRTII4BPP_1BGR, PixelDataFormat.Undefined },
            /* RGB888   */ { SceGxmTextureFormat.U8U8U8_RGB, PixelDataFormat.FormatRgb888 },
            /* RGB888X  */ { SceGxmTextureFormat.U8U8U8X8_RGB1, PixelDataFormat.FormatRgbx8888 },
            /* P4       */ { SceGxmTextureFormat.P4_ABGR, PixelDataFormat.FormatIndexed4 },
            /*          */ { SceGxmTextureFormat.P4_ARGB, PixelDataFormat.FormatIndexed4 },
            /*          */ { SceGxmTextureFormat.P4_RGBA, PixelDataFormat.FormatIndexed4 },
            /*          */ { SceGxmTextureFormat.P4_BGRA, PixelDataFormat.FormatIndexed4 },
            /*          */ { SceGxmTextureFormat.P4_1BGR, PixelDataFormat.FormatIndexed4 },
            /*          */ { SceGxmTextureFormat.P4_1RGB, PixelDataFormat.FormatIndexed4 },
            /*          */ { SceGxmTextureFormat.P4_RGB1, PixelDataFormat.FormatIndexed4 },
            /*          */ { SceGxmTextureFormat.P4_BGR1, PixelDataFormat.FormatIndexed4 },
            /* P8       */ { SceGxmTextureFormat.P8_ABGR, PixelDataFormat.FormatIndexed8 },
            /*          */ { SceGxmTextureFormat.P8_ARGB, PixelDataFormat.FormatIndexed8 },
            /*          */ { SceGxmTextureFormat.P8_RGBA, PixelDataFormat.FormatIndexed8 },
            /*          */ { SceGxmTextureFormat.P8_BGRA, PixelDataFormat.FormatIndexed8 },
            /*          */ { SceGxmTextureFormat.P8_1BGR, PixelDataFormat.FormatIndexed8 },
            /*          */ { SceGxmTextureFormat.P8_1RGB, PixelDataFormat.FormatIndexed8 },
            /*          */ { SceGxmTextureFormat.P8_RGB1, PixelDataFormat.FormatIndexed8 },
            /*          */ { SceGxmTextureFormat.P8_BGR1, PixelDataFormat.FormatIndexed8 },
        };

        static readonly Dictionary<SceGxmTextureFormat, PixelDataFormat> paletteFormatMap = new Dictionary<SceGxmTextureFormat, PixelDataFormat>()
        {
            { SceGxmTextureFormat.P4_ABGR, PixelDataFormat.FormatAbgr8888 },
            { SceGxmTextureFormat.P8_ABGR, PixelDataFormat.FormatAbgr8888 },
            { SceGxmTextureFormat.P4_ARGB, PixelDataFormat.FormatArgb8888 },
            { SceGxmTextureFormat.P8_ARGB, PixelDataFormat.FormatArgb8888 },
            { SceGxmTextureFormat.P4_RGBA, PixelDataFormat.FormatRgba8888 },
            { SceGxmTextureFormat.P8_RGBA, PixelDataFormat.FormatRgba8888 },
            { SceGxmTextureFormat.P4_BGRA, PixelDataFormat.FormatBgra8888 },
            { SceGxmTextureFormat.P8_BGRA, PixelDataFormat.FormatBgra8888 },
            { SceGxmTextureFormat.P4_1BGR, PixelDataFormat.FormatXbgr8888 },
            { SceGxmTextureFormat.P8_1BGR, PixelDataFormat.FormatXbgr8888 },
            { SceGxmTextureFormat.P4_1RGB, PixelDataFormat.FormatXrgb8888 },
            { SceGxmTextureFormat.P8_1RGB, PixelDataFormat.FormatXrgb8888 },
            { SceGxmTextureFormat.P4_RGB1, PixelDataFormat.FormatRgbx8888 },
            { SceGxmTextureFormat.P8_RGB1, PixelDataFormat.FormatRgbx8888 },
            { SceGxmTextureFormat.P4_BGR1, PixelDataFormat.FormatBgrx8888 },
            { SceGxmTextureFormat.P8_BGR1, PixelDataFormat.FormatBgrx8888 },
        };

        static readonly Dictionary<SceGxmTextureBaseFormat, int> bitsPerPixelMap = new Dictionary<SceGxmTextureBaseFormat, int>()
        {
            { SceGxmTextureBaseFormat.U8, 8 },
            { SceGxmTextureBaseFormat.S8, 8 },
            { SceGxmTextureBaseFormat.U4U4U4U4, 16 },
            { SceGxmTextureBaseFormat.U8U3U3U2, 16 },
            { SceGxmTextureBaseFormat.U1U5U5U5, 16 },
            { SceGxmTextureBaseFormat.U5U6U5, 16 },
            { SceGxmTextureBaseFormat.S5S5U6, 16 },
            { SceGxmTextureBaseFormat.U8U8, 16 },
            { SceGxmTextureBaseFormat.S8S8, 16 },
            { SceGxmTextureBaseFormat.U16, 16 },
            { SceGxmTextureBaseFormat.S16, 16 },
            { SceGxmTextureBaseFormat.F16, 16 },
            { SceGxmTextureBaseFormat.U8U8U8U8, 32 },
            { SceGxmTextureBaseFormat.S8S8S8S8, 32 },
            { SceGxmTextureBaseFormat.U2U10U10U10, 32 },
            { SceGxmTextureBaseFormat.U16U16, 32 },
            { SceGxmTextureBaseFormat.S16S16, 32 },
            { SceGxmTextureBaseFormat.F16F16, 32 },
            { SceGxmTextureBaseFormat.F32, 32 },
            { SceGxmTextureBaseFormat.F32M, 32 },
            { SceGxmTextureBaseFormat.X8S8S8U8, 32 },
            { SceGxmTextureBaseFormat.X8U24, 32 },
            { SceGxmTextureBaseFormat.U32, 32 },
            { SceGxmTextureBaseFormat.S32, 32 },
            { SceGxmTextureBaseFormat.SE5M9M9M9, 32 },
            { SceGxmTextureBaseFormat.F11F11F10, 32 },
            { SceGxmTextureBaseFormat.F16F16F16F16, 64 },
            { SceGxmTextureBaseFormat.U16U16U16U16, 64 },
            { SceGxmTextureBaseFormat.S16S16S16S16, 64 },
            { SceGxmTextureBaseFormat.F32F32, 64 },
            { SceGxmTextureBaseFormat.U32U32, 64 },
            { SceGxmTextureBaseFormat.P4, 4 },
            { SceGxmTextureBaseFormat.P8, 8 },
            { SceGxmTextureBaseFormat.U8U8U8, 24 },
            { SceGxmTextureBaseFormat.S8S8S8, 24 },
            { SceGxmTextureBaseFormat.U2F10F10F10, 32 }
        };

        public static PixelDataFormat GetPixelDataFormat(SceGxmTextureFormat pixelFormat)
        {
            if (!formatMap.ContainsKey(pixelFormat)) throw new Exception(string.Format("No matching pixel data format known for {0}", pixelFormat));
            return formatMap[pixelFormat];
        }

        public static PixelDataFormat GetPaletteFormat(SceGxmTextureFormat paletteFormat)
        {
            if (!formatMap.ContainsKey(paletteFormat)) throw new Exception(string.Format("No matching palette format known for {0}", paletteFormat));
            return paletteFormatMap[paletteFormat];
        }

        public static int GetBitsPerPixel(SceGxmTextureBaseFormat baseFormat)
        {
            if (!bitsPerPixelMap.ContainsKey(baseFormat)) throw new Exception(string.Format("No matching bits per pixel known for {0}", baseFormat));
            return bitsPerPixelMap[baseFormat];
        }
    }
}
