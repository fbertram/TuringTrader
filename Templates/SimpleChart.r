#===============================================================================
# Project:     TuringTrader
# Name:        SimpleChart.r
# Description: Template to create simple TuringTrader charts.
# History:     2018xi13, FUB, created
#===============================================================================

#-------------------------------------------------------------------------------
# define helper functions ------------------------------------------------------
#-------------------------------------------------------------------------------

# colors to use for the plots. as the first plot uses column 2, 
# the first value is used for the grid
colors <- c('gray60', 
	    'black', 'red', 'green', 'blue', 'orange', 
	    'cyan', 'darkorchid', 'deeppink', 'gold', 'orangered', 
	    'purple', 'slategray', 'turquoise', 'brown', 'tan')

# function to convert to date
# also see here: https://stackoverflow.com/questions/18178451/is-there-a-way-to-check-if-a-column-is-a-date-in-r
to.date <- function(x) as.Date(as.character(x), format="%m/%d/%Y")

# function to determine if a column has dates in it
is.date <- function(x) (!all(is.na(to.date(x))))

# function to create a pretty plot
create.plot <- function(title, df) {
	# plot the 1st series
	plot(df[,1], 
	     df[,2], ylim = c(min(df[,-1]), max(df[-1])), type = 'l', 
	     axes = TRUE, col = colors[[2]], 
	     xlab = colnames(df)[1], ylab = '', main = title)

	# plot the 2nd and following series
	if (ncol(df) >= 3) {
		for (j in 3:ncol(df)) {
			lines(df[,1], df[,j], col = colors[[j]])
		}
	}

	# set grid, legend, and box
	grid(col = colors[[1]], lty = "dotted")
	legend("topleft", 
	       legend=colnames(df)[2:ncol(df)], col=colors[2:ncol(df)], 
	       lty=1, lwd=2, cex=0.9, bty='n')
	box()
}

# function to create a pretty table
create.table <- function(title, df) {
	# this doesn't work yet. maybe with ggplot...

	# this is what we use for rmarkdown:
	#library(knitr)
	#kable(df, caption = title)
}

# function to handle a single plot/ table
create.plot.or.table <- function(title, csv) {
	df <- read.csv(csv)

	# convert date columns
	fmt <- sapply(df, is.date)
	for (i in 1:ncol(df)) {
		if (fmt[i]) {
			df[,i] <- to.date(df[,i])
		}
	}

	# check for remaining factors
	if (any(sapply(df, is.factor))) {
		create.table(title, df)
	} else {
		create.plot(title, df)
	}
}

#-------------------------------------------------------------------------------
#----- main logic --------------------------------------------------------------
#-------------------------------------------------------------------------------

#----- receive command line argumens 
# command-line arguments come in pairs: the plot name, and the csv with the data
args <- split(commandArgs(trailingOnly = TRUE), 1:2)

#----- set up chart output
chartfile = tempfile("plot", fileext = ".png")
print(sprintf("creating plot '%s'", chartfile))
png(chartfile, width = 1920, height = 1080, unit = "px")

#----- set up page
par(mfrow=c(length(args[[1]]), 1))

#----- create individual plots
for (i in 1:length(args[[1]])) {
	create.plot.or.table(args[[1]][i], args[[2]][i])
}

#----- open chart output
dev.off()
browseURL(chartfile)
Sys.sleep(5) # make sure chart file is open, before R cleans it up

#===============================================================================
# end of file
