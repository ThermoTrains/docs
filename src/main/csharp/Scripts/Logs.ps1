function Get-Tailor {
    param
    (
        [Parameter(Mandatory=$true)]
        [string[]]$files,
        [long]$tail = 100
    )

    function highlight
    {
        Begin
        {
            $settings = @"
{
    "rules": [
        {
            "match": "FATAL",
            "color": "Red"
        },
        {
            "match": "WARNING",
            "color": "Yellow"
        },
        {
            "match": "DEBUG",
            "color": "Gray"
        }
    ],
    "default": {
        "enabled": true,
        "color": "White"
    }
}
"@ | ConvertFrom-Json
        }
        Process
        {
            $matched = $false
            foreach ($setting in $settings.rules) {
                if ($_ -match $setting.match) {
                    $matched = $true
                    Write-Host $_ -ForegroundColor $setting.color
                    break
                }
            }
            if (-not $matched -and $settings.default.enabled)
            {
                Write-Host $_ -ForegroundColor $settings.default.color
            }
        }
    }

    workflow tailor
    {
        param
        (
            [string[]]$files,
            [long]$tail
        )
        foreach -parallel ($file in $files)
        {
            Get-Content -Tail $tail $file -wait
        }
    }

    $ProgressPreference='SilentlyContinue'
    tailor $files $tail | highlight
}

cd C:\Thermobox\logs
$files = dir *.log
Get-Tailor -files $files
