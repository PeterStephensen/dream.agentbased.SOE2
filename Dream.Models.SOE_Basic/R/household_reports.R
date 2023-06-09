rm(list=ls())
library(dplyr)

#install.packages("ContourFunctions")


if(Sys.info()['nodename'] == "C1709161")    # PSP's machine
{
  o_dir = "C:/test/Dream.AgentBased.MacroModel"  
}
if(Sys.info()['nodename'] == "VDI00316")    # Fjernskrivebord
{
  o_dir = "C:/Users/B007566/Documents/Output"  
}
if(Sys.info()['nodename'] == "VDI00382")    # Fjernskrivebord for agentbased projekt
{
  #o_dir = "C:/Users/B007566/Documents/Output" 
  o_dir = "H:/AgentBased/SOE/Output"
  
}
if(Sys.info()['nodename'] == "C2210098")     # Peters nye maskine
{
  o_dir = "C:/Users/B007566/Documents/Output"
}



d_report = read.delim(paste0(o_dir,"/household_reports.txt"))

d_report = d_report %>% arrange(ID)
ids=unique(d_report$ID)
n = length(ids)


pdf(paste0(o_dir,"/household_reports.pdf"))


cols=palette()

#ddd = d_report %>% filter(ID==1345)

dec = function(x,n=3)
{
  z = 10^n
  round(z*x)/z
}

for(i in 1:n)
{

  #i=236
  dr = d_report %>% filter(ID==ids[i])

  if(min(dr$Time)<2035)
    next

  par(mfrow=c(2,2))
  
  cat(i, "/", n, "\n")
  
    
  mx = max(dr$Productivity)
  plot(18+dr$Age/12, dr$Productivity, type="l", xlab="Age", ylab="Productivity", ylim=c(0,1.1*mx))
  abline(h=0)
  
  mx = max(dr$ValConsumption /dr$P_macro)
  mn = min(dr$ValConsumption /dr$P_macro)
  if(mn<0) mn=0
  plot(18+dr$Age/12, dr$ValConsumption /dr$P_macro, type="l", xlab="Age", ylab="Consumption", ylim=c(mn, 1.1*mx))
  lines(18+dr$Age/12, dr$Income /dr$P_macro, col="red")
  abline(h=0)

  z = dr$ValConsumption/dr$Income
  mx = max(z)
  mn = min(z)
  plot(dr$Time, dr$ValConsumption/dr$Income, type="l", xlab="Age", ylab="vC / income", ylim=c(0, 1.1))

  z = dr$Wealth / dr$P_macro
  z2 = dr$Income / dr$P_macro
  mx = max(max(z), max(z2))
  mn = min(min(z), min(z2))
  if(mn>0) mn=0
  plot(dr$Time, dr$Wealth / dr$P_macro, type="l", xlab="Age", ylab="Wealth", ylim=c(mn, mx))
  lines(dr$Time, dr$Income / dr$P_macro, col="red")
  
}

dev.off()
