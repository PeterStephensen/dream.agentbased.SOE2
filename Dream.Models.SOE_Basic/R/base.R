rm(list=ls())
library(dplyr)
library(forecast)
library(data.table)

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
  o_dir = "C:/Users/B007566/Documents/Output"  
  o_dir_svg = "H:/AgentBased/SOE/Graphics"
}

#--------------------------------------

files = list.files(paste0(o_dir, "\\Scenarios\\Macro"), full.names = T)
z = lapply(files, fread)
d0 = rbindlist(z)

#--------------------------------------

#bcol = rgb(0.5,0.8,0.5)
bcol = rgb(0.9,0.9,0.9)

lcol = rgb(0.3,0.5,0.99)
lwd = 1.0


lo = function(x)
{
  z = sort(x)
  n = length(z)
  i = floor(0.025*n)
  d = 0.025*n - i
  if(i==0)
  {
    return(d*z[1])
  }
  else
  {
    return((1-d) * z[i] + d*z[i+1])
  }
}

up = function(x)
{
  z = sort(x, decreasing = T)
  n = length(z)
  i = floor(0.025*n)
  d = 0.025*n - i
  if(i==0)
  {
    return(d*z[1])
  }
  else
  {
    return((1-d) * z[i] + d*z[i+1])
  }
}

pplot = function(zz, sMain, ylab="Relative change", 
                 s_ylim=c(1,1), abs_ylim=c(0,0), type="l", 
                 pch=1, cex=1, lwd=1, zero=F, col=0, horzLine=0, cex.main=1, cex.axis=0.8, cex.lab=1)
{
  
  if(col==0)
    col = rgb(0.9,0.9,0.9)
  
  xcol = rgb(0.5,0.7,0.7)
  mx=max(zz$up)
  mn=min(zz$lo)
  if(zero)
    if(mn>0)
      mn=0
  if(zero)
    if(mx<0)
      mx=0
  
  if(abs_ylim[1]==0 & abs_ylim[2]==0)
  {
    ylim=c(s_ylim[1]*mn,s_ylim[2]*mx)
  }else
  {
    ylim=abs_ylim
  }
  
  plot(zz$Time/12, (zz$up), type="l", col=bcol, ylim=ylim, xlab="Year", cex.axis=cex.axis, 
       ylab=ylab, main=paste(sMain), cex.main=cex.main, cex.lab=cex.lab)
  box(col=col)
  axis(1, col=col, col.ticks=xcol, cex.axis=cex.axis)
  axis(2, col=col, col.ticks=xcol, cex.axis=cex.axis)
  lines(zz$Time/12, zz$lo, col=col)
  polygon(c(zz$Time/12, rev(zz$Time/12)), c(zz$lo, rev(zz$up)), col=bcol, lty=0)
  abline(h=0, col=xcol)
  if(horzLine!=0)
    abline(h=horzLine, col=xcol, lty=2) 
  lines(zz$Time/12, zz$mean, lwd=lwd, col=lcol, type=type, pch=pch, cex=cex)
  
}

vert_lin = function(t)
{
  for(v in seq(from=0, to=max(t), by=10))
    abline(v=v, col="gray")
}

pplot2 = function(t,x, main="", s_miny=0, s_maxy=1.2)
{
  plot(t, x, type="l", main=main, ylim=max(x)*c(s_miny,s_maxy), ylab="", xlab="Year")
  abline(h=0)
  vert_lin(t)
  lines(t, x)
  
}
#--------------------------------------


yr0 = 12*(2105-2014)-1
#yr0 = 12*(2100-2014)-1

d = d0 %>% filter(Time>yr0)
d$Time = d$Time - yr0 

d$Scenario = paste0(d$Scenario, d$Machine)

d = d %>% arrange(Scenario)
ids=unique(d$Scenario)
n = length(ids)

ss=unique(d$Run)
n_ss = length(ss)

#-------------------------
dd = d[is.na(d$marketWage),]
b_ids=unique(dd$Scenario)

d = d %>% filter(!(Scenario %in% b_ids)) 

d$Employment = as.numeric(d$Employment)
d$Production = as.numeric(d$Production)
d$Sales = as.numeric(d$Sales)
#-----------------------------------


db = d %>% filter(Run=="Base") 

db$u = db$nUnemployed/db$nLaborSupply
db$realWage = db$marketWage / db$marketPrice
db$Money = db$marketPrice * db$Sales
db$CapUtil = db$Sales / db$Production

# Example
d_example = db %>% filter(Scenario==ids[2])

show_ex = F

#----------------------------------------------------

calcCorr = function(zz)
{
  n = nrow(zz)
  g = 1.02^(1/12)-1
  (1+g)^(0:(n-1))
  
}

#----------------------------------------------------


pdf(paste0(o_dir, "/base.pdf"))
par(mfrow=c(2,2))
lwd = 0.25

zz = db %>% group_by(Time) %>%
  dplyr::summarize(mean=mean(expSharpeRatio), up=up(expSharpeRatio), lo=lo(expSharpeRatio))

pplot(zz, "Sharpe Ratio", ylab = "", abs_ylim = c(-0.10, 0.10))
if(show_ex) 
  lines(d_example$Time/12, d_example$expSharpeRatio, lwd=lwd)

#--------------
zz = db %>% group_by(Time) %>%
  dplyr::summarize(mean=mean(marketWage), up=up(marketWage), lo=lo(marketWage))

mx = 1.2*max(zz$up)
pplot(zz, "Market Wage", ylab = "", abs_ylim = c(0, mx)) # , abs_ylim = c(-0.05, 0.05)
if(show_ex) 
  lines(d_example$Time/12, d_example$marketWage, lwd=lwd)

#--------------
zz = db %>% group_by(Time) %>%
  dplyr::summarize(mean=mean(realWage), up=up(realWage), lo=lo(realWage))

mx = 1.2*max(zz$up)
pplot(zz, "Real Wage", ylab = "") # , abs_ylim = c(-0.05, 0.05)
if(show_ex) 
  lines(d_example$Time/12, d_example$real, lwd=lwd)


#--------------
zz = db %>% group_by(Time) %>%
  dplyr::summarize(mean=mean(marketPrice), up=up(marketPrice), lo=lo(marketPrice))

corr = calcCorr(zz)

zz$mean = zz$mean*corr
zz$up = zz$up*corr
zz$lo = zz$lo*corr

mx = 1.2*max(zz$up)
pplot(zz, "Market Price (Corrected -2% p.a.)", ylab = "", abs_ylim = c(0, mx)) # 
if(show_ex) 
  lines(d_example$Time/12, corr*d_example$marketPrice, lwd=lwd)

#--------------
zz = db %>% group_by(Time) %>%
  dplyr::summarize(mean=mean(realWage), up=up(realWage), lo=lo(realWage))

corr = calcCorr(zz)

zz$mean = zz$mean/corr
zz$up = zz$up/corr
zz$lo = zz$lo/corr

mx = 1.2*max(zz$up)
pplot(zz, "Real Wage (Corrected 2% p.a.)", ylab = "", abs_ylim = c(0, mx)) # 
if(show_ex) 
  lines(d_example$Time/12, d_example$realWage/corr, lwd=lwd)


#--------------
zz = db %>% group_by(Time) %>%
  dplyr::summarize(mean=mean(marketPrice), up=up(marketPrice), lo=lo(marketPrice))

zz$infl = 0
for(i in 2:nrow(zz))
{
  zz$infl[i] = (zz$mean[i]/zz$mean[i-1])^12 - 1
}

plot(zz$Time, zz$infl, type="l", main="Inflation", ylim=c(-0.026, 0.01), lwd=lwd, ylab="")
abline(h=0)
abline(h= -0.02, lty=2)

#--------------
zz = db %>% group_by(Time) %>%
  dplyr::summarize(mean=mean(marketWage), up=up(marketWage), lo=lo(marketWage))

zz$infl = 0
for(i in 2:nrow(zz))
{
  zz$infl[i] = (zz$mean[i]/zz$mean[i-1])^12 - 1
}

plot(zz$Time, zz$infl, type="l", main="Wage Inflation", ylim=c(-0.026, 0.01), lwd=lwd, ylab="")
abline(h=0)

#--------------
zz = db %>% group_by(Time) %>%
  dplyr::summarize(mean=mean(realWage), up=up(realWage), lo=lo(realWage))

zz$infl = 0
for(i in 2:nrow(zz))
{
  zz$infl[i] = (zz$mean[i]/zz$mean[i-1])^12 - 1
}

plot(zz$Time, zz$infl, type="l", main="Real Wage Inflation", ylim=c(-0.01, 0.026), lwd=lwd, ylab="")
abline(h=0)
abline(h=0.02, lty=2)

#--------------
zz = db %>% group_by(Time) %>%
  dplyr::summarize(mean=mean(u), up=up(u), lo=lo(u))

mx = 1.2*max(zz$up)
pplot(zz, "Unemployment Rate", ylab = "", abs_ylim = c(0, mx)) # , abs_ylim = c(-0.05, 0.05)
if(show_ex) 
  lines(d_example$Time/12, d_example$u, lwd=lwd)

#--------------
zz = db %>% group_by(Time) %>%
  dplyr::summarize(mean=mean(CapUtil), up=up(CapUtil), lo=lo(CapUtil))

mx = 1.2*max(zz$up)
pplot(zz, "Capacity Utilization (Sales / Production)", ylab = "", abs_ylim = c(0.96, 1.0)) # , abs_ylim = c(-0.05, 0.05)
if(show_ex) 
{
  lines(d_example$Time/12, d_example$CapUtil, lwd=lwd)
  abline(h=1, lty=2)
}


#--------------
zz = db %>% group_by(Time) %>%
  dplyr::summarize(mean=mean(nFirms), up=up(nFirms), lo=lo(nFirms))

mx = 1.2*max(zz$up)
pplot(zz, "Number of Firms", ylab = "", abs_ylim = c(0, mx)) # , abs_ylim = c(-0.05, 0.05)
if(show_ex) 
  lines(d_example$Time/12, d_example$nFirms, lwd=lwd)

#--------------
zz = db %>% group_by(Time) %>%
  dplyr::summarize(mean=mean(Employment), up=up(Employment), lo=lo(Employment))

mx = 1.2*max(zz$up)
pplot(zz, "Employment", ylab = "", abs_ylim = c(0, mx)) # , abs_ylim = c(-0.05, 0.05)
if(show_ex) 
  lines(d_example$Time/12, d_example$Employment, lwd=lwd)

#--------------
zz = db %>% group_by(Time) %>%
  dplyr::summarize(mean=mean(Money), up=up(Money), lo=lo(Money))

mx = 1.2*max(zz$up)
pplot(zz, "'Money' (MarkedPrice * Sales)", ylab = "", abs_ylim = c(0, mx)) # , abs_ylim = c(-0.05, 0.05)
if(show_ex) 
  lines(d_example$Time/12, d_example$Money, lwd=lwd)

#--------------
zz = db %>% group_by(Time) %>%
  dplyr::summarize(mean=mean(MeanAge/12), up=up(MeanAge/12), lo=lo(MeanAge/12))

mx = 1.2*max(zz$up)
pplot(zz, "Mean Firm Age (Years)", ylab = "", abs_ylim = c(0, mx)) # , abs_ylim = c(-0.05, 0.05)
if(show_ex) 
  lines(d_example$Time/12, d_example$MeanAge, lwd=lwd)

#--------------
zz = db %>% group_by(Time) %>%
  dplyr::summarize(mean=mean(Sales), up=up(Sales), lo=lo(Sales))

corr = calcCorr(zz)

zz$mean = zz$mean/corr
zz$up = zz$up/corr
zz$lo = zz$lo/corr

mx = 1.2*max(zz$up)
pplot(zz, "Sales (Corrected 2% p.a.)", ylab = "", abs_ylim = c(0, mx)) # 
if(show_ex) 
  lines(d_example$Time/12, d_example$Sales/corr, lwd=lwd)

#--------------
zz = db %>% group_by(Time) %>%
  dplyr::summarize(mean=mean(Production), up=up(Production), lo=lo(Production))

corr = calcCorr(zz)

zz$mean = zz$mean/corr
zz$up = zz$up/corr
zz$lo = zz$lo/corr

mx = 1.2*max(zz$up)
pplot(zz, "Production (Corrected 2% p.a.)", ylab = "", abs_ylim = c(0, mx)) # 
if(show_ex) 
  lines(d_example$Time/12, d_example$Production/corr, lwd=lwd)


dev.off()




