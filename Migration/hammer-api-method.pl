#!/usr/bin/perl
use strict;
use warnings;
use LWP;
use HTTP::Request::Common;
use JSON::XS;
use POSIX qw(strftime);

my $log_fh;

# Auto flush the default buffer.
$| = 1;

if (scalar @ARGV eq 3) {
    my $now_string = strftime "%Y%m%d%H%M%S ", localtime;
    my $log_filename = "movelog$now_string.txt";
    open ($log_fh, '>', $log_filename) or die "Failed to open file '$log_filename' for writing.";
    
    my $url = $ARGV[0];
    my $paramName = $ARGV[1];
    my $file = $ARGV[2];
    open(my $fh, '<:encoding(UTF-8)', $file) or die "Could not open file '$file' $!\n";
    
    print_everywhere("URL = $url\n");
    print_everywhere("FILE = $file\n");
    print_everywhere("\n");
    
    while (my $nodeId = <$fh>) {
        chomp $nodeId;
        print_everywhere("Calling POST API method with value = $nodeId...   ");
        my $ua = LWP::UserAgent->new;
        my $res = $ua->request(POST $url, [$paramName => $nodeId]);
        if ($res->is_success) {
            my $res_data = JSON::XS->new->decode($res->content);
            if ($res_data->{ "Success" }) {
                print_everywhere("SUCCESS\n");
            } else {
                print_everywhere("FAILED\n");
                print_everywhere("REASON: " . $res_data->{ "Message" } . "\n");
            }
        } else {
            print_everywhere("HTTP POST request failed.\n");
        }
    }
    
    close $fh;
} elsif (scalar @ARGV == 1) {
    print "Missing argument FILE.\n";
    print_help();
} elsif (scalar @ARGV == 0) {
    print "Missing arguments URL and FILE.\n";
    print_help();
} else {
    print "Unsupported number of arguments.\n";
    print_help();
}

sub print_help {
    print "\n";
    print "Usage: hammer-api-method.pl [URL] [PARAMETERNAME] [FILE]\n";
    print "Read values from specified file and call the POST API method at the given URL with each value.\n";
}

sub print_everywhere {
    my ($text) = @_;
    
    print $log_fh $text;
    print $text;
}