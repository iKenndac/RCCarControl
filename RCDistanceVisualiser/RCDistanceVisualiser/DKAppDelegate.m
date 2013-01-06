//
//  DKAppDelegate.m
//  RCDistanceVisualiser
//
//  Created by Daniel Kennett on 05/01/2013.
//  Copyright (c) 2013 Daniel Kennett. All rights reserved.
//

#import "DKAppDelegate.h"

@interface DKAppDelegate ()

@property (nonatomic, readwrite, strong) NSTimer *pollTimer;

@end

@implementation DKAppDelegate

- (void)applicationDidFinishLaunching:(NSNotification *)aNotification
{
	// Insert code here to initialize your application
}

-(IBAction)connectToEndpoint:(id)sender {

	NSString *endpoint = [[NSUserDefaults standardUserDefaults] valueForKey:@"Endpoint"];
	if (endpoint.length == 0) {
		[[NSAlert alertWithMessageText:@"Invalid endpoint"
						 defaultButton:@"OK"
					   alternateButton:@""
						   otherButton:@""
			 informativeTextWithFormat:@"Please enter a valid endpoint URL"]
		 beginSheetModalForWindow:self.window modalDelegate:nil didEndSelector:nil contextInfo:nil];
		return;
	}

	[self startPolling];
}

-(void)startPolling {
	[self stopPolling];
	self.pollTimer = [NSTimer scheduledTimerWithTimeInterval:0.25
													  target:self
													selector:@selector(updateFromEndpoint:)
													userInfo:nil
													 repeats:YES];
}

-(void)stopPolling {
	[self.pollTimer invalidate];
	self.pollTimer = nil;
}

-(void)updateFromEndpoint:(NSTimer *)aTimer {

	NSString *endpoint = [[NSUserDefaults standardUserDefaults] valueForKey:@"Endpoint"];
	if (endpoint.length == 0) {
		[self stopPolling];
		return;
	}

	NSURL *endpointURL = [NSURL URLWithString:endpoint];

	NSURLRequest *request = [NSURLRequest requestWithURL:endpointURL
											 cachePolicy:NSURLRequestReloadIgnoringLocalCacheData
										 timeoutInterval:0.2];

	[NSURLConnection sendAsynchronousRequest:request
									   queue:[NSOperationQueue mainQueue]
						   completionHandler:^(NSURLResponse *response, NSData *data, NSError *error) {

							   if (error != nil) {
								   [[NSAlert alertWithError:error] beginSheetModalForWindow:self.window modalDelegate:nil didEndSelector:nil contextInfo:nil];
								   [self stopPolling];
								   return;
							   }

							   NSHTTPURLResponse *httpResponse = (NSHTTPURLResponse *)response;
							   if (httpResponse.statusCode != 200) {
								   [[NSAlert alertWithMessageText:@"Endpoint returned error"
													defaultButton:@"OK"
												  alternateButton:@""
													  otherButton:@""
										informativeTextWithFormat:@"The error was: %@.", @(httpResponse.statusCode)]
									beginSheetModalForWindow:self.window modalDelegate:nil didEndSelector:nil contextInfo:nil];
								   [self stopPolling];
								   return;
							   }

							   NSDictionary *values = [NSJSONSerialization JSONObjectWithData:data
																					  options:0
																						error:nil];

							   NSDictionary *distanceValues = values[@"distances"];
							   NSUInteger frontLeft = [distanceValues[@"FrontLeftDistance"] unsignedIntegerValue];
							   NSUInteger frontMiddle = [distanceValues[@"FrontMiddleDistance"] unsignedIntegerValue];
							   NSUInteger frontRight = [distanceValues[@"FrontRightDistance"] unsignedIntegerValue];
							   NSUInteger rear = [distanceValues[@"RearDistance"] unsignedIntegerValue];

							   self.carView.frontLeftDistance = frontLeft;
							   self.carView.frontMiddleDistance = frontMiddle;
							   self.carView.frontRightDistance = frontRight;
							   self.carView.rearDistance = rear;

							   [self.carView setNeedsDisplay:YES];

							   NSDictionary *accelerometerValues = values[@"accelerometer"];
							   double x = [accelerometerValues[@"x"] doubleValue];
							   double y = [accelerometerValues[@"y"] doubleValue];
							   double z = [accelerometerValues[@"z"] doubleValue];

							   self.orientationView.x = x;
							   self.orientationView.y = y;
							   self.orientationView.z = z;
							   [self.orientationView setNeedsDisplay:YES];
							   
						   }];
}

@end
