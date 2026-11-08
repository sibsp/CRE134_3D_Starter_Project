# Double-click Make-Class-Diagram.bat. Do not run this file on its own.
# Put both files in the Unity project folder, next to the Assets folder.
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$Assets = Join-Path $ProjectRoot 'Assets'
if (-not (Test-Path $Assets)) {
    Write-Host ''
    Write-Host 'Put Make-Class-Diagram.bat in your Unity project folder.'
    Write-Host 'That is the folder which contains Assets. It is not inside Assets.'
    exit 1
}

# Folders that are imported packs, not student scripts.
$SkipFolders = @(
    'TextMesh Pro',
    'Starter Assets',
    'TutorialInfo',
    'PBS Materials Variety Pack',
    'Plugins',
    'Examples & Extras'
)

function Strip-ForParse([string]$Text) {
    $Text = [regex]::Replace($Text, '(?s)/\*.*?\*/', ' ')
    $Text = [regex]::Replace($Text, '(?m)//.*?$', ' ')
    $Text = [regex]::Replace($Text, '@"(?:""|[^"])*"', '""')
    $Text = [regex]::Replace($Text, '"(?:\\.|[^"\\])*"', '""')
    do {
        $next = [regex]::Replace($Text, '\[(?=[A-Za-z])[^\[\]]*\]', ' ')
        $changed = $next -ne $Text
        $Text = $next
    } while ($changed)
    return $Text
}

function Convert-PlantType([string]$TypeName) {
    if ([string]::IsNullOrWhiteSpace($TypeName)) { return 'object' }
    $t = ($TypeName -replace '\s+', '')
    $t = $t -replace '<', '~'
    $t = $t -replace '>', '~'
    return $t
}

function Get-TypeNames([string]$TypeName) {
    $clean = $TypeName -replace '\[\]', ''
    return [regex]::Matches($clean, '[A-Za-z_][A-Za-z0-9_]*') | ForEach-Object { $_.Value }
}

function Get-Members([string]$Body) {
    $fields = New-Object System.Collections.ArrayList
    $methods = New-Object System.Collections.ArrayList
    $depth = 0
    $buf = New-Object System.Text.StringBuilder
    for ($i = 0; $i -lt $Body.Length; $i++) {
        $c = $Body[$i]
        if ($c -eq '{') {
            if ($depth -eq 0) {
                $sig = ($buf.ToString() -replace '\s+', ' ').Trim()
                $buf.Clear() | Out-Null
                $ahead = ''
                if ($i + 1 -lt $Body.Length) { $ahead = $Body.Substring($i + 1).TrimStart() }
                if ($sig -match '\bclass\b' -or $sig -match '\bstruct\b' -or $sig -match '\benum\b') {
                    # Nested types are collected on their own.
                }
                elseif ($sig -match '\bpublic\b') {
                    $isProperty = $ahead -match '^(get|set)\b'
                    [void]$methods.Add([pscustomobject]@{ Signature = $sig; IsProperty = $isProperty })
                }
            }
            $depth++
            continue
        }
        if ($c -eq '}') {
            if ($depth -gt 0) { $depth-- }
            continue
        }
        if ($depth -eq 0) {
            if ($c -eq ';') {
                $stmt = ($buf.ToString() -replace '\s+', ' ').Trim()
                $buf.Clear() | Out-Null
                if ($stmt -match '\bpublic\b' -and $stmt -notmatch '\b(class|struct|enum|delegate)\b') {
                    [void]$fields.Add($stmt)
                }
            }
            else {
                [void]$buf.Append($c)
            }
        }
    }
    return [pscustomobject]@{ Fields = $fields; Methods = $methods }
}

function Format-Field([string]$Statement) {
    $m = [regex]::Match($Statement, '^(?:public\s+)(?<mods>(?:(?:static|const|readonly|volatile)\s+)*)(?<type>[\w\.<>,\[\]]+(?:\s*\[\s*\])?)\s+(?<name>\w+)\s*(?:=.*)?$')
    if (-not $m.Success) { return $null }
    $mods = ''
    if ($m.Groups['mods'].Value -match 'static|const') { $mods = '{static} ' }
    $typeName = Convert-PlantType $m.Groups['type'].Value
    return [pscustomobject]@{
        Line = "+ $mods$($m.Groups['name'].Value) : $typeName"
        Type = $m.Groups['type'].Value
        Name = $m.Groups['name'].Value
    }
}

function Format-Method($Entry) {
    $sig = $Entry.Signature
    if ($Entry.IsProperty) {
        $m = [regex]::Match($sig, '^(?:public\s+)(?<mods>(?:(?:static|virtual|override|abstract)\s+)*)(?<type>[\w\.<>,\[\]]+)\s+(?<name>\w+)\s*$')
        if (-not $m.Success) { return $null }
        $mods = ''
        if ($m.Groups['mods'].Value -match 'static') { $mods = '{static} ' }
        return "+ $mods$($m.Groups['name'].Value) : $(Convert-PlantType $m.Groups['type'].Value)"
    }
    $m = [regex]::Match($sig, '^(?:public\s+)(?<mods>(?:(?:static|virtual|override|abstract|async|sealed|new)\s+)*)(?<type>[\w\.<>,\[\]]+)\s+(?<name>\w+)\s*\((?<params>.*)\)\s*$')
    if (-not $m.Success) { return $null }
    $name = $m.Groups['name'].Value
    if ($name -in @('if', 'for', 'foreach', 'while', 'switch', 'catch', 'lock', 'using', 'get', 'set')) { return $null }
    $mods = ''
    if ($m.Groups['mods'].Value -match 'static') { $mods = '{static} ' }
    $paramBits = @()
    $rawParams = $m.Groups['params'].Value.Trim()
    if ($rawParams) {
        $paramParts = @()
        $paramBuf = New-Object System.Text.StringBuilder
        $angleDepth = 0
        foreach ($ch in $rawParams.ToCharArray()) {
            if ($ch -eq '<') { $angleDepth++ }
            elseif ($ch -eq '>' -and $angleDepth -gt 0) { $angleDepth-- }
            if ($ch -eq ',' -and $angleDepth -eq 0) {
                $paramParts += $paramBuf.ToString()
                $paramBuf.Clear() | Out-Null
                continue
            }
            [void]$paramBuf.Append($ch)
        }
        if ($paramBuf.Length -gt 0) { $paramParts += $paramBuf.ToString() }
        foreach ($part in $paramParts) {
            $part = ($part -replace '\b(ref|out|in|params)\b', '').Trim()
            if (-not $part) { continue }
            $bits = $part -split '\s+'
            if ($bits.Count -ge 2) {
                $paramBits += "$($bits[-1]):$(Convert-PlantType ($bits[0..($bits.Count - 2)] -join ''))"
            }
            else {
                $paramBits += (Convert-PlantType $bits[0])
            }
        }
    }
    $argText = $paramBits -join ', '
    return "+ $mods$name($argText) : $(Convert-PlantType $m.Groups['type'].Value)"
}

$files = Get-ChildItem -Path $Assets -Filter *.cs -Recurse -File | Where-Object {
    $full = $_.FullName
    foreach ($skip in $SkipFolders) {
        if ($full -like "*\$skip\*") { return $false }
    }
    return $true
}

$classes = New-Object System.Collections.ArrayList
foreach ($file in $files) {
    $raw = Get-Content -Path $file.FullName -Raw -ErrorAction SilentlyContinue
    if (-not $raw) { continue }
    $text = Strip-ForParse $raw
    $relative = $file.FullName.Substring($Assets.Length).TrimStart('\')
    $top = $relative.Split('\')[0]
    if ($top -like '*.cs') { $package = 'Scripts' } else { $package = $top }
    $matches = [regex]::Matches($text, '(?s)\bclass\s+(?<name>\w+)\s*(?::\s*(?<bases>[^{]+?))?\s*\{')
    foreach ($match in $matches) {
        $open = $match.Index + $match.Length - 1
        $depth = 0
        $end = $open
        for ($i = $open; $i -lt $text.Length; $i++) {
            if ($text[$i] -eq '{') { $depth++ }
            elseif ($text[$i] -eq '}') {
                $depth--
                if ($depth -eq 0) { $end = $i; break }
            }
        }
        $body = $text.Substring($open + 1, [Math]::Max(0, $end - $open - 1))
        $header = $text.Substring([Math]::Max(0, $match.Index - 80), [Math]::Min(80, $match.Index))
        $isStatic = $header -match 'static\s*$' -or $text.Substring([Math]::Max(0, $match.Index - 40), [Math]::Min(40, $match.Index)) -match '\bstatic\s+$'
        $isPublic = $text.Substring([Math]::Max(0, $match.Index - 40), [Math]::Min(40, $match.Index)) -match '\b(public|internal)\s+(?:static\s+|sealed\s+|abstract\s+|partial\s+)*$'
        if (-not $isPublic) { continue }
        $bases = @()
        if ($match.Groups['bases'].Success) {
            $bases = $match.Groups['bases'].Value -split ',' | ForEach-Object { ($_ -replace '\s+', ' ').Trim() } | Where-Object { $_ }
        }
        $parent = $null
        foreach ($other in $classes) {
            if ($file.FullName -eq $other.File -and $match.Index -gt $other.Open -and $match.Index -lt $other.End) {
                $parent = $other.Name
            }
        }
        $parsed = Get-Members $body
        [void]$classes.Add([pscustomobject]@{
            Name = $match.Groups['name'].Value
            Package = $package
            File = $file.FullName
            Bases = @($bases)
            Static = [bool]$isStatic
            Parent = $parent
            Fields = @($parsed.Fields)
            Methods = @($parsed.Methods)
            Open = $open
            End = $end
        })
    }
}

if ($classes.Count -eq 0) {
    Write-Host ''
    Write-Host 'No public classes were found under Assets.'
    Write-Host 'Check that your scripts are in the Assets folder.'
    exit 1
}

$known = @{}
foreach ($c in $classes) { $known[$c.Name] = $true }

$lines = New-Object System.Collections.ArrayList
[void]$lines.Add('@startuml')
[void]$lines.Add('title Class diagram')
[void]$lines.Add('skinparam classAttributeIconSize 0')
[void]$lines.Add('skinparam shadowing false')
[void]$lines.Add('hide empty members')
[void]$lines.Add('')

$relations = New-Object System.Collections.ArrayList
foreach ($package in ($classes | Select-Object -ExpandProperty Package -Unique)) {
    [void]$lines.Add("package $package {")
    foreach ($c in ($classes | Where-Object { $_.Package -eq $package })) {
        $stereo = ''
        if ($c.Static) { $stereo = ' <<static>>' }
        [void]$lines.Add("  class $($c.Name)$stereo {")
        $seenField = @{}
        foreach ($statement in $c.Fields) {
            $field = Format-Field $statement
            if (-not $field) { continue }
            $links = @(Get-TypeNames $field.Type | Where-Object { $known.ContainsKey($_) -and $_ -ne $c.Name })
            if ($links.Count -gt 0) {
                foreach ($target in $links) {
                    [void]$relations.Add("$($c.Name) --> `"$($field.Name)`" $target")
                }
            }
            elseif (-not $seenField.ContainsKey($field.Name)) {
                [void]$lines.Add("    $($field.Line)")
                $seenField[$field.Name] = $true
            }
        }
        foreach ($entry in $c.Methods) {
            $methodLine = Format-Method $entry
            if ($methodLine) { [void]$lines.Add("    $methodLine") }
        }
        [void]$lines.Add('  }')
        foreach ($base in $c.Bases) {
            $baseName = ($base -split '\s+')[0]
            $baseName = ($baseName -split '<')[0]
            if ($baseName -match '^I[A-Z]') {
                [void]$relations.Add("$baseName <|.. $($c.Name)")
            }
            else {
                [void]$relations.Add("$baseName <|-- $($c.Name)")
            }
        }
        if ($c.Parent) { [void]$relations.Add("$($c.Parent) +-- $($c.Name)") }
    }
    [void]$lines.Add('}')
    [void]$lines.Add('')
}

[void]$lines.Add('')
foreach ($rel in ($relations | Select-Object -Unique)) { [void]$lines.Add($rel) }
[void]$lines.Add('@enduml')

$pumlPath = Join-Path $ProjectRoot 'ClassDiagram.puml'
$svgPath = Join-Path $ProjectRoot 'ClassDiagram.svg'
$puml = ($lines -join "`n") + "`n"
[System.IO.File]::WriteAllText($pumlPath, $puml, [System.Text.UTF8Encoding]::new($false))

function Encode-PlantUml([string]$Text) {
    $inputBytes = [System.Text.Encoding]::UTF8.GetBytes($Text)
    $memory = New-Object System.IO.MemoryStream
    $deflate = New-Object System.IO.Compression.DeflateStream($memory, [System.IO.Compression.CompressionMode]::Compress, $true)
    $deflate.Write($inputBytes, 0, $inputBytes.Length)
    $deflate.Dispose()
    $compressed = $memory.ToArray()
    $alphabet = '0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-_'
    $sb = New-Object System.Text.StringBuilder
    for ($i = 0; $i -lt $compressed.Length; $i += 3) {
        $remain = [Math]::Min(3, $compressed.Length - $i)
        $b1 = [int]$compressed[$i]
        $b2 = 0
        $b3 = 0
        if ($remain -gt 1) { $b2 = [int]$compressed[$i + 1] }
        if ($remain -gt 2) { $b3 = [int]$compressed[$i + 2] }
        [void]$sb.Append($alphabet[($b1 -shr 2) -band 63])
        [void]$sb.Append($alphabet[((($b1 -band 3) -shl 4) -bor ($b2 -shr 4))])
        if ($remain -gt 1) { [void]$sb.Append($alphabet[((($b2 -band 15) -shl 2) -bor ($b3 -shr 6))]) }
        if ($remain -gt 2) { [void]$sb.Append($alphabet[$b3 -band 63]) }
    }
    return $sb.ToString()
}

$url = 'https://www.plantuml.com/plantuml/svg/' + (Encode-PlantUml $puml)
try {
    Invoke-WebRequest -Uri $url -UseBasicParsing -OutFile $svgPath | Out-Null
}
catch {
    Write-Host ''
    Write-Host 'The picture could not be downloaded.'
    Write-Host 'Check the internet, then double-click Make-Class-Diagram.bat again.'
    Write-Host 'The diagram text is saved as ClassDiagram.puml'
    exit 1
}

$svgHead = Get-Content -Path $svgPath -TotalCount 1 -ErrorAction SilentlyContinue
if (-not $svgHead -or ($svgHead -notlike '*<svg*')) {
    Write-Host ''
    Write-Host 'The diagram site did not return a picture.'
    Write-Host 'Check the internet, then try again.'
    exit 1
}

Start-Process $svgPath
Write-Host "Classes found: $($classes.Count)"
Write-Host "Picture: $svgPath"
exit 0
