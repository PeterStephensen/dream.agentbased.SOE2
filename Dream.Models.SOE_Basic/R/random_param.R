rm(list=ls())
#install.packages("rjson")

library(dplyr)
library(forecast)
library(data.table)
library(rjson)


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

#d_prod = read.delim(paste0(o_dir,"/data_firms.txt"))




files = list.files(paste0(o_dir, "\\Scenarios\\Macro"), full.names = T)
files_json = list.files(paste0(o_dir, "\\Scenarios\\Settings"), full.names = T)

StartYear = 2014
EndYear = 2215
n_time = 12*(1+EndYear-StartYear) - 1

n = length(files_json)
crash = rep(F, n) 
InvestorProfitSensitivity  = rep(0, n) 
PriceMarkup = rep(0, n)
PriceMarkupSensi = rep(0, n)
PriceMarkdown = rep(0, n)
PriceMarkdownSensi = rep(0, n)
WageMarkup = rep(0, n)
WageMarkupSensi = rep(0, n)
WageMarkdown = rep(0, n)
WageMarkdownSensi = rep(0, n)
len = rep(0,n) 

pb = txtProgressBar(min = 0, max = n, initial = 0) 

#for(i in 1:50)
for(i in 1:n)
{
  #i=10
  #cat(i,"\n")
  setTxtProgressBar(pb,i)

  json_d = fromJSON(file = files_json[i])
  s = sub(".json", ".txt", files_json[i])
  s = sub("Settings", "Macro", s)
  d0 = read.delim(file = s)

  InvestorProfitSensitivity[i] = json_d$InvestorProfitSensitivity
  PriceMarkdown[i] = json_d$FirmPriceMarkdown
  PriceMarkdownSensi[i] = json_d$FirmPriceMarkdownSensitivity
  PriceMarkup[i] = json_d$FirmPriceMarkup
  PriceMarkupSensi[i] = json_d$FirmPriceMarkupSensitivity
  WageMarkdown[i] = json_d$FirmWageMarkdown
  WageMarkdownSensi[i] = json_d$FirmWageMarkdownSensitivity
  WageMarkup[i] = json_d$FirmWageMarkup
  WageMarkupSensi[i] = json_d$FirmWageMarkupSensitivity

  crash[i] = max(d0$Time) < n_time
  len[i] = nrow(d0) 
        
}
close(pb)


model <- glm(crash ~ PriceMarkdown+PriceMarkdownSensi+PriceMarkup+PriceMarkupSensi
             + WageMarkdown+WageMarkdownSensi+WageMarkup+WageMarkupSensi
             +InvestorProfitSensitivity ,family=binomial(link='logit'))


summary(model)

hist(PriceMarkdown[crash])
hist(PriceMarkup[crash])
hist(WageMarkup[crash])
hist(WageMarkdown[crash])

hist(WageMarkupSensi[crash])
hist(WageMarkdownSensi[crash])

hist(PriceMarkupSensi[crash])
hist(PriceMarkdownSensi[crash])

#---------------------------------------

i=0

i=i+1

json_d = fromJSON(file = files_json[i])

s0 = sub(".json", ".txt", files_json[i])
s0 = sub("Settings", "Macro", s0)
s1 = sub("base_", "count_Productivity_", s0)

d0 = read.delim(file = s0)
d1 = read.delim(file = s1)

#plot(d1$nFirms / d0$nFirms - 1, type="l")
#abline(h=0.1, lty=2)
#abline(h=0)

plot(d1$Production / d0$Production - 1, type="l")
abline(h=0.1, lty=2)
abline(h=0)

lines(d1$Production, col="red")

#---------------------------------------


n = length(files_json)
crash = rep(F, n) 
InvestorProfitSensitivity  = rep(0, n) 
PriceMarkup = rep(0, n)
PriceMarkupSensi = rep(0, n)
PriceMarkdown = rep(0, n)
PriceMarkdownSensi = rep(0, n)
WageMarkup = rep(0, n)
WageMarkupSensi = rep(0, n)
WageMarkdown = rep(0, n)
WageMarkdownSensi = rep(0, n)

g_prod = rep(0, n)
base = rep(FALSE, n)
crash = rep(FALSE, n)
no_good = rep(FALSE, n)
g_diff = rep(FALSE, n)

pb = txtProgressBar(min = 0, max = n, initial = 0) 
for(i in 1:n)
{
  #i=1
  #cat(i,"\n")
  setTxtProgressBar(pb,i)
  
  
  if(grepl("base_", files_json[i], fixed = TRUE))
  {
    json_d = fromJSON(file = files_json[i])
    
    InvestorProfitSensitivity[i] = json_d$InvestorProfitSensitivity
    PriceMarkdown[i] = json_d$FirmPriceMarkdown
    PriceMarkdownSensi[i] = json_d$FirmPriceMarkdownSensitivity
    PriceMarkup[i] = json_d$FirmPriceMarkup
    PriceMarkupSensi[i] = json_d$FirmPriceMarkupSensitivity
    WageMarkdown[i] = json_d$FirmWageMarkdown
    WageMarkdownSensi[i] = json_d$FirmWageMarkdownSensitivity
    WageMarkup[i] = json_d$FirmWageMarkup
    WageMarkupSensi[i] = json_d$FirmWageMarkupSensitivity
    
    s0 = sub(".json", ".txt", files_json[i])
    s0 = sub("Settings", "Macro", s0)
    s1 = sub("base_", "count_Productivity_", s0)

    d0 = read.delim(file = s0)
    d1 = read.delim(file = s1)
    
    if(nrow(d0)==n_time+1)
    {
      base[i] = T
      if(nrow(d1)<n_time+1)
        crash[i] = T
    }
      
    if(nrow(d0)==n_time+1 & nrow(d1)==n_time+1)
    {
      d0 = tail(d0, 1000)
      d1 = tail(d1, 1000)
      
      d = merge(d0,d1, by="Time")
      
      if(is.numeric(d$Production.x) & is.numeric(d$Production.y))
      {
        g_prod[i] = mean(d$Production.y /d$Production.x-1)
        
        g_diff[i] = mean((d$Production.y /d$Production.x - 1.1)^2)^0.5
        
      }
    }
  }
}
close(pb)

g_prod[base & !crash]

hist(g_diff[g_diff>0 & g_diff<1], breaks=20)
hist(log((g_prod[g_prod!=0]-0.1)^2), breaks=20)

z = g_prod!=0
y = log((g_prod[z]-0.1)^2)

InvestorProfitSensitivity = InvestorProfitSensitivity[z]
PriceMarkdown		  = PriceMarkdown[z]
PriceMarkdownSensi	  = PriceMarkdownSensi[z]
PriceMarkup		  = PriceMarkup[z]
PriceMarkupSensi	  = PriceMarkupSensi[z]
WageMarkdown		  = WageMarkdown[z]
WageMarkdownSensi	  = WageMarkdownSensi[z]
WageMarkup		  = WageMarkup[z]
WageMarkupSensi		  = WageMarkupSensi[z]

lm1 = lm(y ~ PriceMarkdown+PriceMarkdownSensi+PriceMarkup+PriceMarkupSensi
         + WageMarkdown+WageMarkdownSensi+WageMarkup+WageMarkupSensi
         +InvestorProfitSensitivity)

summary(lm1)


hist(g2, breaks=20)





json_d = fromJSON(file = files_json[i])

s = sub(".json", ".txt", files_json[i])
s = sub("Settings", "Macro", s)
d0 = read.delim(file = s)

InvestorProfitSensitivity[i] = json_d$InvestorProfitSensitivity
PriceMarkdown[i] = json_d$FirmPriceMarkdown
PriceMarkdownSensi[i] = json_d$FirmPriceMarkdownSensitivity
PriceMarkup[i] = json_d$FirmPriceMarkup
PriceMarkupSensi[i] = json_d$FirmPriceMarkupSensitivity
WageMarkdown[i] = json_d$FirmWageMarkdown
WageMarkdownSensi[i] = json_d$FirmWageMarkdownSensitivity
WageMarkup[i] = json_d$FirmWageMarkup
WageMarkupSensi[i] = json_d$FirmWageMarkupSensitivity

crash[i] = max(d0$Time) < n_time
len[i] = nrow(d0) 



