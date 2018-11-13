#===============================================================================
# Project:     TuringTrader
# Name:        SimpleChart.r
# Description: Template to create simple TuringTrader charts.
# History:     2018xi13, FUB, created
#===============================================================================

#----- receive command line argumens 
args = commandArgs(trailingOnly=TRUE)
if (length(args) == 0) {
	stop("At least one argument must be supplied (input file).n",
	     call.=FALSE)
}

#----- set up chart output
chartfile = tempfile("plot", fileext = ".png")
print(sprintf("creating plot '%s'", chartfile))
png(chartfile, width = 1920, height = 1080, unit = "px")

#----- set up page
par(mfrow=c(length(args), 1))

#----- create individual plots
for (i in 1:length(args)) {
	df<-read.csv(args[i])
	x<-df[,1]
	y<-df[,-1]
	matplot(x, y, type="l", lty=1)
        #title(main=\"{0}\",xlab=\"{1}\",ylab=\"\")
	#legend(\"bottom\",legend=colnames(y), col=seq_len(ncol(y)), cex=0.8, fill=seq_len(ncol(y)))"
}

#----- open chart output
dev.off()
browseURL(chartfile)
Sys.sleep(5) # make sure chart file is open, before R cleans it up

#===============================================================================
# end of file
