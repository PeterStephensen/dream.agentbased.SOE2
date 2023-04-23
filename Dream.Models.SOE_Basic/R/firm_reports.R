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


d_report = read.delim(paste0(o_dir,"/file_reports.txt")) %>% filter(Production>0)

d_report$EmploymentMarkup = as.numeric(d_report$EmploymentMarkup)


#ID=34151
d_report = d_report %>% arrange(ID)
ids=unique(d_report$ID)
n = length(ids)

cols=palette()

#ddd = d_report %>% filter(ID==1345)

dec = function(x,n=3)
{
  z = 10^n
  round(z*x)/z
}

d_report$Time_f = as.factor(d_report$Time)
d_tot = d_report %>% group_by(Time_f) %>% summarise(Employment=sum(Employment, na.rm = T)) %>%
  mutate(Time=as.numeric(as.character(Time_f)))

pdf(paste0(o_dir,"/firm_reports.pdf"))
par(mfrow=c(3,3))


for(i in 1:n)
{
  #i=334
  #i=i+1
  #i=which(ids==34852)
  #i=i+1
  dr = d_report %>% filter(ID==ids[i])

  if(nrow(dr)<12*5)
    next
  
  if(T)
  {
    if(dr$Productivity[1] < 1.8)
      next
  }
  
  if(F)
  {
    if(nrow(dr)>12*10)
    {
      dr = dr[1:(12*10),]
    }
    
  }
  
  if(sum(dr$Profit / dr$Price>0)==0)
    next
  
  cat(i, "/", n, "\n")
  
  #d_tot1 = d_tot %>% filter(Time>min(dr$Time), Time<max(dr$Time))
  
  #mx_tot = max(d_tot1$Employment)
  mx = max(max(dr$Employment), max(dr$OptimalEmployment))
  plot(dr$Time, dr$Employment, type="s", ylab="Employment", xlab="Time", main="", col=cols[1], ylim=c(0,1.1*mx))
  #lines(d_tot1$Time, 0.3*mx*d_tot1$Employment/mx_tot, col="gray")
  lines(dr$Time, dr$OptimalEmployment, col=cols[2], type="s")
  lines(dr$Time, dr$ExpectedEmployment, col=cols[3], type="s")
  abline(v=2050, lty=2)
  abline(h=0)
  ContourFunctions::multicolor.title(c("Actual employment ","Optimal employment", " Expected employment"), 1:3, cex.main = 0.7)

  mx = max(max(dr$ExpectedPotentialSales), max(dr$PotensialSales))
  plot(dr$Time, dr$PotensialSales, type="s", ylab="Optimal Production", main="", 
       xlab="Time", col=cols[1], ylim=c(0,1.1*mx))
  lines(dr$Time, dr$OptimalProduction, col=cols[2], type="l")
  lines(dr$Time, dr$ExpectedPotentialSales, col=cols[3], type="l")
  abline(h=0)
  ContourFunctions::multicolor.title(c("Potensial sales "," Optimal production", " Expected potensial sales"), 1:3, cex.main = 0.7)
  

  mx = max(max(dr$expApplications), max(dr$expQuitters+dr$ExpectedVacancies))
  plot(dr$Time, dr$expApplications, type="s", ylim=c(0,mx), xlab="Time", ylab="", main="", col=cols[1])
  lines(dr$Time, dr$expQuitters+dr$ExpectedVacancies, lty=1, col=cols[2])
  lines(dr$Time, dr$expQuitters, type="l", col=cols[3])
  abline(h=0)
  abline(v=2050, lty=2)
  ContourFunctions::multicolor.title(c("ExpApplications ", "ExpQuitters+ ExpVacancies"), 1:2, cex.main = 0.7)

  if(F)
  {
    gg = (last(dr$ExpectedWage)/first(dr$ExpectedWage))^(1/(12*(last(dr$Time)-first(dr$Time))))-1
    corr = (1+gg)^(0:(12*(last(dr$Time)-first(dr$Time)))-1)
    mx = max(dr$Wage / dr$ExpectedWage[1]/corr)
    mn = min(dr$Wage / dr$ExpectedWage[1]/corr)
    plot(dr$Time, dr$Wage / dr$ExpectedWage[1] / corr, type="s", ylab="Wage", 
         main="", xlab="Time", col=cols[3], ylim=c(0.8*mn, 1.2*mx))   #
    lines(dr$Time, dr$ExpectedWage / dr$ExpectedWage[1] / corr, lty=1)
    
    gg = (last(dr$ExpectedPrice)/first(dr$ExpectedPrice))^(1/(12*(last(dr$Time)-first(dr$Time))))-1
    corr = (1+gg)^(0:(12*(last(dr$Time)-first(dr$Time)))-1)
    mx = max(dr$Price / dr$ExpectedPrice[1]/corr)  
    mn = min(dr$Price / dr$ExpectedPrice[1]/corr)
    plot(dr$Time, dr$Price / dr$ExpectedPrice[1]/corr, type="s", ylab="Price", main="", 
         xlab="Time", col=cols[3], ylim=c(0.8*mn, 1.2*mx)) 
    lines(dr$Time, dr$ExpectedPrice / dr$ExpectedPrice[1]/corr, lty=1)
    
  

    t = dr$Time[-1]
    x = dr$Wage[-1]
    xx = dr$ExpectedWage[-nrow(dr)]
    plot(t,  x / xx, type="l", main="Relative Wage", ylab="Relative", col=cols[3])
    abline(h=1)

    t = dr$Time[-1]
    x = dr$Price[-1]
    xx = dr$ExpectedPrice[-nrow(dr)]
    plot(t,  x / xx, type="l", main="Relative Price", ylab="Relative", col=cols[3])
    abline(h=1)
    
  }
  
  plot(dr$Time,  dr$RelativeWage, type="l", main="Relative Wage", ylab="Relative", col=cols[3], ylim=c(0.8,1.2))
  abline(h=1)
  
  plot(dr$Time,  dr$RelativePrice, type="l", main="Relative Price", ylab="Relative", col=cols[3], ylim=c(0.8,1.2))
  abline(h=1)
  
  plot(dr$Time, dr$Vacancies, type="s", ylab="Vacancies", main="", xlab="Time", col=cols[3])
  lines(dr$Time, dr$ExpectedVacancies, col=cols[2])
  #lines(dr$Time, dr$expApplications, col=cols[1])
  abline(v=2050, lty=2)
  abline(h=0)

  if(sum(is.nan(dr$Profit / dr$Price))==0)
  {
    plot(dr$Time, dr$Profit / dr$Price, type="s", ylab="Profit / Price", xlab="Time", 
         main="", cex.main=0.9, col=cols[3])
    abline(h=0)
    abline(v=2050, lty=2)
  }
  else
  {
    plot(0)
  }

  #mx = max(dr$MarketPrice / dr$ExpectedPrice[1])  
  #mn = min(dr$MarketPrice / dr$ExpectedPrice[1])
  #plot(dr$Time, dr$MarketPrice / dr$ExpectedPrice[1], type="l", ylab="Price", main="", 
  #     xlab="Time", col=cols[3], ylim=c(0.9*mn, 1.1*mx)) 
  #lines(dr$Time, dr$ExpectedPrice / dr$ExpectedPrice[1], lty=2)
  
  
  #mx = max(max(dr$Production), max(dr$OptimalProduction))
  #plot(dr$Time, dr$Production, type="s", ylab="Sale", main="", 
  #     xlab="Time", col=cols[1], ylim=c(0,1.1*mx))
  #lines(dr$Time, dr$OptimalProduction, col=cols[3], type="l")
  #abline(h=0)
  #ContourFunctions::multicolor.title(c("Production ","Optimal production"), 1:2, cex.main = 0.7)
  

  mx=max(dr$EmploymentMarkup[-1])
  mn=min(dr$EmploymentMarkup[-1])
  plot(dr$Time[-1], dr$EmploymentMarkup[-1], type="l", col=cols[3], ylim=c(0,1.1*mx), ylab="Employment Markup")  
  

    
  #plot(0)
  
  
  plot.new()

  d = 0.15
  text(0,1-d*0, "Date:", adj=0, cex=0.8)  
  text(0,1-d*1, "ID:", adj=0, cex=0.8)  
  text(0,1-d*2, "Productivity:", adj=0, cex=0.8)  
  text(0,1-d*3, "Start time:", adj=0, cex=0.8)  
  text(0,1-d*4, "End time:", adj=0, cex=0.8)  
  text(0,1-d*5, "Time span:", adj=0, cex=0.8)  
  
  text(0.5,1-d*0, date(), adj=0, cex=0.8)  
  text(0.5,1-d*1, ids[i], adj=0, cex=0.8)  
  text(0.5,1-d*2, dec(dr$Productivity[2], 2), adj=0, cex=0.8)  
  text(0.5,1-d*3, dec(dr$Time[1], 1), adj=0, cex=0.8)  
  text(0.5,1-d*4, dec(dr$Time[nrow(dr)], 1), adj=0, cex=0.8)  
  text(0.5,1-d*5, dec(dr$Time[nrow(dr)]-dr$Time[1], 1), adj=0, cex=0.8)  

}



dev.off()
