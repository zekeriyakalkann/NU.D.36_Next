# Checks the working copies in Exports\Lab4_Phase3_Working against Lab4UiAssets.Specs (read from the C# file).
# Reports size, real alpha, residual checkerboard-like pixels and the Specs verdict. Writes Logs\lab4_phase3_working_verify.txt.
param([string]$Dir = 'Exports\Lab4_UI', [string]$LogName = 'lab4_ui_verify.txt')
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$dir = Join-Path $root $Dir
$specText = Get-Content (Join-Path $root 'NU.D.36_Unity\Assets\Scripts\Editor\Lab4UiAssets.cs') -Raw
$specs = @{}
foreach ($m in [regex]::Matches($specText, 'new Spec\("([^"]+)", (\d+), (\d+)(?:, ([^)]*))?\)')) {
    $specs[$m.Groups[1].Value] = @{ w = [int]$m.Groups[2].Value; h = [int]$m.Groups[3].Value; opt = $m.Groups[4].Value }
}
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System; using System.Drawing; using System.Drawing.Imaging; using System.Runtime.InteropServices;
public static class Chk {
  public static string Run(string p, out int w, out int h, out int minA, out double transp) {
    using (var b = new Bitmap(p)) {
      w = b.Width; h = b.Height;
      var d = b.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
      var a = new byte[w * h * 4];
      for (int y = 0; y < h; y++) Marshal.Copy(IntPtr.Add(d.Scan0, y * d.Stride), a, y * w * 4, w * 4);
      b.UnlockBits(d);
      minA = 255; long z = 0, resid = 0;
      for (int i = 0; i < w * h; i++) {
        int A = a[i * 4 + 3]; if (A < minA) minA = A; if (A == 0) z++;
        if (A > 200) { int B = a[i*4], G = a[i*4+1], R = a[i*4+2]; int mx = Math.Max(R, Math.Max(G, B)), mn = Math.Min(R, Math.Min(G, B)); if (mx - mn <= 8 && (R+G+B)/3 >= 200) resid++; }
      }
      transp = (double)z / (w * h);
      return resid.ToString();
    }
  }
}
'@ -ReferencedAssemblies System.Drawing
$lines = @('file | size | spec size | alpha min | fully transparent | residual neutral-light px | verdict')
foreach ($f in Get-ChildItem $dir -Filter *.png) {
    $w = 0; $h = 0; $minA = 0; $t = 0.0
    $res = [Chk]::Run($f.FullName, [ref]$w, [ref]$h, [ref]$minA, [ref]$t)
    $s = $specs[$f.Name]
    if ($s) {
        $needsAlpha = $s.opt -match 'needsAlpha: true'; $opaque = $s.opt -match 'opaque: true'
        $sizeOk = ($w -eq $s.w -and $h -eq $s.h); $alphaOk = if ($needsAlpha) { $minA -lt 255 } elseif ($opaque) { $minA -eq 255 } else { $true }
        $verdict = if ($sizeOk -and $alphaOk) { 'MATCHES Specs' } else { 'DOES NOT MATCH: ' + $(if (-not $sizeOk) { 'size ' }) + $(if (-not $alphaOk) { 'alpha' }) }
        $spec = "$($s.w)x$($s.h)"
    } else { $verdict = 'no Specs entry (working source, not for Assets/UI)'; $spec = '-' }
    $lines += "$($f.Name) | ${w}x$h | $spec | $minA | $('{0:P1}' -f $t) | $res | $verdict"
}

# extra checks for the originally produced sprites
Add-Type -TypeDefinition @'
using System; using System.Drawing;
public static class Extra {
  // compass: mean colour difference across the wrap seam vs between two ordinary neighbouring columns
  public static string Seam(string p) {
    using (var b = new Bitmap(p)) {
      double seam = 0, ref1 = 0, ref2 = 0; int h = b.Height, w = b.Width;
      for (int y = 0; y < h; y++) {
        Color a = b.GetPixel(w - 1, y), c = b.GetPixel(0, y), d1 = b.GetPixel(w / 2, y), d2 = b.GetPixel(w / 2 + 1, y), e1 = b.GetPixel(w / 3, y), e2 = b.GetPixel(w / 3 + 1, y);
        seam += Math.Abs(a.R - c.R) + Math.Abs(a.G - c.G) + Math.Abs(a.B - c.B);
        ref1 += Math.Abs(d1.R - d2.R) + Math.Abs(d1.G - d2.G) + Math.Abs(d1.B - d2.B);
        ref2 += Math.Abs(e1.R - e2.R) + Math.Abs(e1.G - e2.G) + Math.Abs(e1.B - e2.B);
      }
      return string.Format("wrap-seam diff {0:F1} vs neighbouring-column diffs {1:F1} / {2:F1} (per row, RGB sum)", seam / h, ref1 / h, ref2 / h);
    }
  }
  // needle / knob: farthest visible pixel from the canvas centre and the alpha-weighted centroid offset
  public static string Radius(string p) {
    using (var b = new Bitmap(p)) {
      double max = 0; int w = b.Width, h = b.Height; double cx = (w - 1) / 2.0, cy = (h - 1) / 2.0;
      for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if (b.GetPixel(x, y).A > 8) max = Math.Max(max, Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)));
      return string.Format("farthest visible pixel from centre {0:F1} px (canvas half-size {1})", max, w / 2);
    }
  }
}
'@ -ReferencedAssemblies System.Drawing
$lines += ''
if (Test-Path (Join-Path $dir 'pusula-seridi.png')) { $lines += 'pusula-seridi.png: ' + [Extra]::Seam((Join-Path $dir 'pusula-seridi.png')) }
if (Test-Path (Join-Path $dir 'gosterge-ibre.png')) { $lines += 'gosterge-ibre.png: ' + [Extra]::Radius((Join-Path $dir 'gosterge-ibre.png')) }
$log = Join-Path $root "Logs\$LogName"
$lines | Set-Content -Encoding utf8 $log
$lines
