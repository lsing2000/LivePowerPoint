<#
 Changes demo data every -IntervalSeconds so a slideshow visibly updates. Requires the data service running in Development.
 Usage: .\change-demo-data.ps1 [-Base http://127.0.0.1:5080] [-ApiKey demo-key-change-me] [-IntervalSeconds 15] [-Count 3]
#>
param([string]$Base="http://127.0.0.1:5080",[string]$ApiKey="demo-key-change-me",[int]$IntervalSeconds=15,[int]$Count=3)
$h=@{ "X-Api-Key"=$ApiKey }
for($i=0;$i -lt $Count;$i++){
  $v=Get-Random -Minimum 50000 -Maximum 200000
  Invoke-RestMethod -Method Post -Headers $h -Uri "$Base/api/demo/update?datasetId=sales-summary&key=North&value=$v" | Out-Null
  Invoke-RestMethod -Method Post -Headers $h -Uri "$Base/api/demo/update?datasetId=inventory&key=Widgets&value=$([int]($v/300))" | Out-Null
  "{0:o} North sales -> {1}" -f (Get-Date),$v
  if($i -lt $Count-1){ Start-Sleep -Seconds $IntervalSeconds }
}
